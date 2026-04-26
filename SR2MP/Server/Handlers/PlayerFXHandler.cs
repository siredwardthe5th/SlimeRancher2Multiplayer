using System.Net;
using SR2MP.Packets.FX;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerFX)]
public sealed class PlayerFXHandler : BasePacketHandler<PlayerFXPacket>
{
    public PlayerFXHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    // Per-player active vac trail instance — see client handler for rationale.
    private static readonly Dictionary<string, GameObject> _activeTrails = new();

    protected override void Handle(PlayerFXPacket packet, IPEndPoint clientEp)
    {
        // Display the FX on the host (host is also a player) before forwarding
        // to other clients. All lookups are TryGet to tolerate missing/null
        // entries (e.g. WaterSplash visual on builds where no prefab name
        // matched the substring search). Whole block is wrapped in try/catch
        // so a failed FX render never aborts the broadcast.
        handlingPacket = true;
        try
        {
            // Vac trail is a toggleable visual on the remote player's body —
            // mirror the client-side handler's special case before falling
            // into the dictionary-driven sound/visual paths.
            if (packet.FX == PlayerFXType.VacTrailStart || packet.FX == PlayerFXType.VacTrailEnd)
            {
                HandleVacTrail(packet);
            }
            else if (IsPlayerSoundDictionary.TryGetValue(packet.FX, out var isSound))
            {
                if (!isSound)
                {
                    if (fxManager.PlayerFXMap.TryGetValue(packet.FX, out var fxPrefab) && fxPrefab)
                        FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
                }
                else if (fxManager.PlayerAudioCueMap.TryGetValue(packet.FX, out var cue) && cue)
                {
                    var transient = ShouldPlayerSoundBeTransientDictionary.TryGetValue(packet.FX, out var t) && t;
                    var volume = PlayerSoundVolumeDictionary.TryGetValue(packet.FX, out var v) ? v : 1f;
                    var loop = DoesPlayerSoundLoopDictionary.TryGetValue(packet.FX, out var l) && l;

                    if (!playerObjects.TryGetValue(packet.Player ?? string.Empty, out var src) || !src)
                    {
                        // No player object on host (e.g. join still in progress) — skip render.
                    }
                    else if (transient)
                    {
                        RemoteFXManager.PlayTransientAudio(cue, src.transform.position, volume);
                    }
                    else
                    {
                        var playerAudio = src.GetComponent<SECTR_PointSource>();
                        if (playerAudio)
                        {
                            playerAudio.Cue = cue;
                            playerAudio.Loop = loop;
                            playerAudio.instance.Volume = volume;
                            playerAudio.Play();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"PlayerFXHandler error (ignored): {ex.Message}");
        }
        finally { handlingPacket = false; }

        Main.Server.SendToAllExcept(packet, clientEp);
    }

    private static void HandleVacTrail(PlayerFXPacket packet)
    {
        var key = packet.Player ?? string.Empty;
        var hasPlayer = playerObjects.TryGetValue(key, out var src);
        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-VacFX] (server) HandleVacTrail fx={packet.FX} player='{packet.Player}' hasPlayer={hasPlayer} src={(src ? src.name : "<null>")}");

        if (!hasPlayer || !src) return;

        _activeTrails.TryGetValue(key, out var existing);

        if (packet.FX == PlayerFXType.VacTrailEnd)
        {
            if (existing) FXHelpers.RecycleAndStopFX(existing);
            _activeTrails.Remove(key);
            return;
        }

        if (existing) return;
        _activeTrails.Remove(key);

        fxManager.PlayerFXMap.TryGetValue(PlayerFXType.VacTrail, out var prefab);
        if (!prefab)
        {
            prefab = fxManager.RefreshVacTrailPrefab();
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-VacFX] (server) VacTrailStart: cached prefab was null, refreshed -> {(prefab ? prefab.name : "<still null>")}");
        }
        if (!prefab) return;

        // ─────────────────────────────────────────────────────────────────
        // ACTIVE: chest-attached. See Client/Handlers/PlayerFXHandler.cs and
        // Patches/FX/OnVacuumFXLifecycle.cs for the full rationale.
        //
        // Bone-attached alternative (preserved for future re-attempt):
        /*
        if (Main.DiagnosticLogging && _hierarchyDumped.Add(packet.Player ?? string.Empty))
            RemoteFXManager.DumpHierarchy(src.transform, $"(server) player={packet.Player}");

        var attachTo = RemoteFXManager.FindRightHandTransform(src.transform);
        var parent = attachTo != null ? attachTo.gameObject : src;

        var instance = FXHelpers.SpawnAndPlayFX(prefab, parent);
        if (instance != null)
        {
            instance.name = "SR2MP_VacTrail";
            instance.transform.localPosition = attachTo != null ? Vector3.zero : new Vector3(0f, 1.2f, 0f);
            if (attachTo == null) instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(true);

            var ps = instance.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play(true);

            _activeTrails[key] = instance;

            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-VacFX] (server) VacTrailStart spawned '{prefab.name}' on '{parent.name}' (handBone={(attachTo != null ? attachTo.name : "<none>")} active={instance.activeInHierarchy} hasPS={ps != null})");
        }
        */
        // ─────────────────────────────────────────────────────────────────

        var instance = FXHelpers.SpawnAndPlayFX(prefab, src);
        if (instance != null)
        {
            instance.name = "SR2MP_VacTrail";
            instance.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(true);

            var ps = instance.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play(true);

            _activeTrails[key] = instance;

            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-VacFX] (server) VacTrailStart spawned '{prefab.name}' on '{src.name}' (active={instance.activeInHierarchy} hasPS={ps != null})");
        }
    }

    // Tracked players we've already dumped the bone hierarchy for. Uncomment
    // alongside the bone-attach code path above if re-enabling.
    //
    // private static readonly HashSet<string> _hierarchyDumped = new();
}
