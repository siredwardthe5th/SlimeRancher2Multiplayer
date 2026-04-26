using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Packets.FX;

namespace SR2MP.Patches.FX;

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
