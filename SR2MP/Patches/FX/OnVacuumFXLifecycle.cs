using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Packets.FX;

namespace SR2MP.Patches.FX;

// =============================================================================
// VAC TRAIL SYNC — STATUS & INVESTIGATION HISTORY
// =============================================================================
//
// CURRENT STATE: broadcast + audio + chest-attached visual all work. The visual
// appears in front of the remote player's torso and tracks body rotation. It
// does NOT track the gun's hand pose — that's a known limitation, see below.
//
// File layout (all comments cross-reference each other):
//   - OnVacuumFXLifecycle.cs (this file): the BROADCAST side — patches that
//     detect when the local player is vacuuming and emit packets.
//   - Client/Handlers/PlayerFXHandler.cs: RECEIVE side for clients. Has the
//     active chest-attach code AND commented-out bone-attach attempts.
//   - Server/Handlers/PlayerFXHandler.cs: RECEIVE side for the host. Mirror
//     of the client handler.
//   - Shared/Managers/RemoteFXManager.cs: prefab lookup + commented-out
//     bone-finding helpers (FindRightHandTransform, DumpHierarchy).
//
// =============================================================================
// HISTORY: BROADCAST SIDE — what fires the VacTrailStart/End packets
// =============================================================================
//
//   1. [HarmonyPatch(typeof(VacuumInteractionFX), "OnEnable" / "OnDisable")]
//      The original disabled patch from the upstream codebase. SR2 1.2.0 doesn't
//      have OnEnable/OnDisable on VacuumInteractionFX (verified via SrInspect
//      methods dump). Harmony couldn't install. Dead end.
//
//   2. [HarmonyPatch(typeof(VacuumInteractionFX), "set_vacActive")]
//      vacActive is a Boolean property on VacuumInteractionFX. Tried Prefix
//      capturing the prior value + Postfix detecting transitions. Il2CppInterop
//      classifies set_vacActive as a "field accessor" and refuses to patch:
//        "Method ...set_vacActive is a field accessor, it can't be patched"
//      The fallback Harmony backend then errors on parameter binding because
//      the IL2CPP wrapper exposes the parameter as `A_1` not `value`. Dead end.
//
//   3. Postfix on VacuumInteractionFX.Update polling vacActive. Patch installed
//      but vacActive never changed in testing — turns out it doesn't mean "the
//      player is currently vacuuming", it means "this FX object exists in the
//      scene" (always true once spawned). Dead end.
//
//   4. CURRENT: Postfix on VacuumItem.Update polling _vacPressed. VacuumItem
//      is the player's vac weapon component (Player.PlayerItems namespace),
//      and _vacPressed is the actual "player holding vac button" boolean.
//      Per-instance cache keyed on IL2CPP Pointer detects transitions and
//      broadcasts. Same VacuumItem class is what OnPlayVacAudio already hooks
//      for sound, which is why audio worked from the start.
//
// =============================================================================
// HISTORY: RECEIVE SIDE — how the remote player visually shows the vac trail
// =============================================================================
//
// Original (upstream) handler treated VacTrailStart/End as one-shot sounds via
// IsPlayerSoundDictionary. That's wrong — they need to be a TOGGLEABLE visual
// on the remote player. We special-cased VacTrailStart/End before the dict path
// and routed them to a new HandleVacTrail helper.
//
// HandleVacTrail prefab choice journey:
//
//   - PS VacuumDirectional: spawned successfully, reported active=True, but
//     emitted nothing visible. It's a directional emitter that needs the
//     vacuum aim/distance shader inputs (vacWindDistance etc.) to actually
//     emit. Detached from the vac aim logic, it's inert.
//
//   - FX_Vac_Dust / FX_Vac_Rings / Droplets_Vacced: simple ambient particle
//     systems that emit on Play() without external inputs. FX_Vac_Dust got
//     picked and IS visible. Current choice.
//
// Cache eviction problem: PlayerFXMap[VacTrail] resolved at Initialize() but
// became null later — Unity destroyed the cached GameObject reference (the
// "FX Vac Dust" we grabbed was a transient Clone). Fix: RemoteFXManager
// .RefreshVacTrailPrefab() re-scans Resources.FindObjectsOfTypeAll on null
// and updates the cache.
//
// =============================================================================
// HISTORY: ATTACH POINT — where on the remote player the FX is parented
// =============================================================================
//
// IDEAL: parent the FX to the bone that holds the vac, so it tracks the gun's
// pose perfectly. Tried hard. Couldn't get it working. Reverted to chest
// attachment. Bone-attach code is preserved as comments in the receive
// handlers for future re-attempts.
//
// What we tried (DumpHierarchy of BeatrixMainMenu(Clone) confirmed all of
// these names exist on the rig):
//
//   1. rWristJ (right wrist bone) — would track the arm pose. Not present in
//      our chosen-prefab heuristic search at first (we used "wrist_r" patterns;
//      SR2 names them "rWristJ"). Once we matched, this became a candidate
//      but a lower priority than vac-mesh attachment.
//
//   2. mesh_vacpac (the third-person vac model) — would be the obvious choice,
//      BUT: the BeatrixMainMenu rig parents mesh_vacpac to MainHipJ (hip),
//      not to a hand bone. So attaching the FX there gives "tracks body
//      rotation but not arm animation". Also: when used directly, the FX
//      inherits the vac body's local rotation which is offset/tilted relative
//      to the FX prefab's expected forward — visible result was a tilted,
//      sideways-pointing trail.
//
//   3. joint11 (the END of the hose chain mesh_vacpac → mesh_vacpac_hose →
//      joint1 → ... → joint11) — should be the muzzle, with forward axis
//      pointing along the hose direction (i.e. where the vac shoots). Spawned
//      with active=False because joint11 lives under mesh_vacpac_hose which
//      is INACTIVE in the BeatrixMainMenu rig (probably only activated when
//      the gun is actively held in a way the preview model doesn't simulate).
//      Unity's activeInHierarchy cascades, so the FX child inherits inactive
//      state and never renders.
//
//   4. anchor_joint21 — sibling of mesh_vacpac_hose, likely the hose's fixed
//      anchor. Untested in isolation; presumably has same active-hierarchy
//      issues since it's under the same parent chain.
//
// Tracking-dict bug found during investigation: Transform.Find is direct-
// children-only. Original code used src.transform.Find("SR2MP_VacTrail") to
// locate the existing FX for idempotent-Start and End-recycle. Worked when
// FX was parented to player root, broke immediately when we started parenting
// to deep bones — Find returned null, every Start spawned a new instance
// (the "extra glitched out static vac fx that was not present on the host"
// symptom), End couldn't find anything to recycle ("animation also didn't
// stop when the vac was stopped"). Fix: replaced with per-player
// Dictionary<string, GameObject> _activeTrails, which is dispose-correct
// regardless of where in the hierarchy the FX is parented.
//
// =============================================================================
// FUTURE WORK ideas (in roughly increasing order of effort)
// =============================================================================
//
//   - Try a ROTATION OFFSET when attached to mesh_vacpac. The visible tilt is
//     a fixed rotation; if we measure it once we can compensate. Quaternion
//     localRotation = Quaternion.Euler(-90, 0, 0) or similar.
//
//   - Find a different prefab that doesn't have a strong directional bias
//     (FX_Vac_Rings or Droplets_Vacced were untried; they MIGHT look right
//     ambient-attached without rotation gymnastics).
//
//   - Broadcast SpawnVacuumRipple events: the local player fires ~5/s while
//     vacuuming. Each ripple has a position. Send those as one-shot world FX
//     packets, receive plays a ripple at that position. Replaces the trail
//     metaphor with discrete ripple visuals — accurate but chatty (5 packets
//     per second per vacuuming player).
//
//   - Replace the BeatrixMainMenu remote player model with one that has the
//     vac properly hand-parented. This would also fix the "vac doesn't move
//     with arm" issue inherently. Big change, affects more than this feature.
//
// =============================================================================
//
// Vac suction trail visual sync. History of dead ends:
//   1. Patch OnEnable/OnDisable on VacuumInteractionFX — those methods don't
//      exist on SR2 1.2.0. Patch install failed.
//   2. Patch set_vacActive on VacuumInteractionFX — Il2CppInterop classifies
//      it as a pure field accessor and refuses to patch.
//   3. Poll vacActive on VacuumInteractionFX in its Update Postfix — patch
//      installed but `vacActive` doesn't actually toggle when the player
//      vacuums. It seems to mean "this FX object is active in scene" (always
//      true once spawned), not "player is currently holding vac". No
//      transitions ever fired in testing.
//
// What actually works: VacuumItem._vacPressed is the real player-holding-vac
// signal (lives on Player.PlayerItems.VacuumItem, the player's vac weapon
// component, NOT on the visual FX class). Same OnPlayVacAudio audio sync
// hooks VacuumItem.PlayTransientAudio, which is why audio cues sync but the
// trail did not.
//
// Polling _vacPressed in VacuumItem.Update Postfix gets us transitions
// within one frame. Per-instance cache keyed on IL2CPP pointer; silent on
// first observation so we don't blast a phantom Start/End on join.
[HarmonyPatch(typeof(VacuumItem), nameof(VacuumItem.Update))]
internal static class OnVacuumItemUpdate
{
    private static readonly Dictionary<IntPtr, bool> _lastPressed = new();

    public static void Postfix(VacuumItem __instance)
    {
        if (handlingPacket) return;
        if (!MultiplayerActive) return;

        var current = __instance._vacPressed;
        var key = __instance.Pointer;
        var hadPrev = _lastPressed.TryGetValue(key, out var prev);
        _lastPressed[key] = current;

        if (!hadPrev) return;
        if (prev == current) return;

        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-VacFX] _vacPressed {prev}->{current} broadcasting");

        Main.SendToAllOrServer(new PlayerFXPacket
        {
            FX = current ? PlayerFXType.VacTrailStart : PlayerFXType.VacTrailEnd,
            Player = LocalID,
        });
    }
}
