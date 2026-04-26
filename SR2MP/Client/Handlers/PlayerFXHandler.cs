using SR2MP.Packets.FX;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.PlayerFX)]
public sealed class PlayerFXHandler : BaseClientPacketHandler<PlayerFXPacket>
{
    public PlayerFXHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    // Per-player active vac trail instance. Originally tracked via a named
    // child on the player root, but switching to bone-attached parenting
    // broke that since Transform.Find is direct-children-only. The dict is
    // keyed on player ID so disposal works regardless of where the FX is
    // parented in the model hierarchy.
    private static readonly Dictionary<string, GameObject> _activeTrails = new();

    protected override void Handle(PlayerFXPacket packet)
    {
        handlingPacket = true;
        try
        {
            // Vac trail is a *toggleable* visual on the remote player's body,
            // not a one-shot like every other PlayerFX. Special-case it
            // before falling into the sound/visual dictionary path (which
            // routes both Start/End into a no-op audio cue lookup).
            if (packet.FX == PlayerFXType.VacTrailStart || packet.FX == PlayerFXType.VacTrailEnd)
            {
                HandleVacTrail(packet);
                return;
            }

            if (!IsPlayerSoundDictionary.TryGetValue(packet.FX, out var isSound))
                return;

            if (!isSound)
            {
                if (fxManager.PlayerFXMap.TryGetValue(packet.FX, out var fxPrefab) && fxPrefab)
                    FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);

                // Pair WaterSplash visual with its audio cue at the same position.
                if (packet.FX == PlayerFXType.WaterSplash
                    && fxManager.PlayerAudioCueMap.TryGetValue(PlayerFXType.WaterSplashSound, out var splashCue)
                    && splashCue)
                {
                    RemoteFXManager.PlayTransientAudio(splashCue, packet.Position, 0.7f);
                }
                return;
            }

            if (!fxManager.PlayerAudioCueMap.TryGetValue(packet.FX, out var cue) || !cue)
                return;

            if (ShouldPlayerSoundBeTransientDictionary.TryGetValue(packet.FX, out var transient) && transient)
            {
                if (!playerObjects.TryGetValue(packet.Player ?? string.Empty, out var src) || !src) return;
                var volume = PlayerSoundVolumeDictionary.TryGetValue(packet.FX, out var v) ? v : 1f;
                RemoteFXManager.PlayTransientAudio(cue, src.transform.position, volume);
            }
            else
            {
                if (!playerObjects.TryGetValue(packet.Player ?? string.Empty, out var src) || !src) return;
                var playerAudio = src.GetComponent<SECTR_PointSource>();
                if (!playerAudio) return;

                playerAudio.Cue = cue;
                playerAudio.Loop = DoesPlayerSoundLoopDictionary.TryGetValue(packet.FX, out var loop) && loop;
                playerAudio.instance.Volume = PlayerSoundVolumeDictionary.TryGetValue(packet.FX, out var vol) ? vol : 1f;
                playerAudio.Play();
            }
        }
        catch
        {
            // ignore — diagnostic-only path; missing FX should not crash the client
        }
        handlingPacket = false;
    }

    private static void HandleVacTrail(PlayerFXPacket packet)
    {
        var key = packet.Player ?? string.Empty;
        var hasPlayer = playerObjects.TryGetValue(key, out var src);
        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-VacFX] HandleVacTrail received fx={packet.FX} player='{packet.Player}' hasPlayer={hasPlayer} src={(src ? src.name : "<null>")}");

        if (!hasPlayer || !src) return;

        _activeTrails.TryGetValue(key, out var existing);

        if (packet.FX == PlayerFXType.VacTrailEnd)
        {
            if (existing)
            {
                FXHelpers.RecycleAndStopFX(existing);
                _activeTrails.Remove(key);
                if (Main.DiagnosticLogging)
                    SrLogger.LogMessage("[SR2MP-Diag-VacFX] VacTrailEnd: recycled existing trail");
            }
            else
            {
                _activeTrails.Remove(key); // clean stale dead refs
                if (Main.DiagnosticLogging)
                    SrLogger.LogMessage("[SR2MP-Diag-VacFX] VacTrailEnd: no existing trail to remove");
            }
            return;
        }

        // VacTrailStart — idempotent. Also recycle a stale dead ref before
        // spawning so we don't leak when Unity destroys the parent (e.g.
        // scene change) without firing End.
        if (existing)
        {
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage("[SR2MP-Diag-VacFX] VacTrailStart: trail already present, skipping");
            return;
        }
        _activeTrails.Remove(key);

        fxManager.PlayerFXMap.TryGetValue(PlayerFXType.VacTrail, out var prefab);
        if (!prefab)
        {
            // Cached prefab GameObject was destroyed by Unity since Initialize()
            // ran (commonly after the original FX prototype was reaped). Re-scan
            // currently-loaded particle renderers and update the cache.
            prefab = fxManager.RefreshVacTrailPrefab();
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-VacFX] VacTrailStart: cached prefab was null, refreshed -> {(prefab ? prefab.name : "<still null>")}");
        }
        if (!prefab) return;

        // One-time hierarchy dump per player so we can verify bone names if
        // attach point still looks wrong (only runs when DiagnosticLogging
        // is on AND we haven't dumped this player yet).
        if (Main.DiagnosticLogging && _hierarchyDumped.Add(packet.Player ?? string.Empty))
            RemoteFXManager.DumpHierarchy(src.transform, $"player={packet.Player}");

        // Prefer to attach to the right-hand bone so the FX tracks the gun.
        // If the lookup misses, fall back to the player root with a chest
        // offset (visible but stationary).
        var attachTo = RemoteFXManager.FindRightHandTransform(src.transform);
        var parent = attachTo != null ? attachTo.gameObject : src;

        var instance = FXHelpers.SpawnAndPlayFX(prefab, parent);
        if (instance != null)
        {
            instance.name = "SR2MP_VacTrail";
            instance.transform.localPosition = attachTo != null ? Vector3.zero : new Vector3(0f, 1.2f, 0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(true);

            var ps = instance.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play(true);

            _activeTrails[key] = instance;

            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-VacFX] VacTrailStart spawned '{prefab.name}' on '{parent.name}' (handBone={(attachTo != null ? attachTo.name : "<none>")} active={instance.activeInHierarchy} hasPS={ps != null})");
        }
        else if (Main.DiagnosticLogging)
        {
            SrLogger.LogMessage($"[SR2MP-Diag-VacFX] VacTrailStart: SpawnAndPlayFX returned null for prefab '{prefab.name}'");
        }
    }

    // Tracks which players we've already dumped the bone hierarchy for so we
    // don't spam the log on every vac press.
    private static readonly HashSet<string> _hierarchyDumped = new();
}