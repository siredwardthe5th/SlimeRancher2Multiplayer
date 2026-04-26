using MelonLoader;
using SR2MP.Packets.Landplot;

namespace SR2MP.Components.World;

// Periodic safety net for SlimeFeeder.FeedSpeed sync, mirroring the design
// of SiloReconciler.
//
// Existing event-driven coverage:
//   - SetFeederSpeed Postfix (programmatic callers)
//   - StepFeederSpeed Postfix (UI dial click)
//
// Both fire reliably in testing. But the user reported that speed dial
// changes "do not always sync" — symptoms didn't match a packet-loss or
// ordering problem (every packet arrived in order), so the most plausible
// explanations are (a) some other code path mutates the speed without going
// through either patched method, or (b) the receiver's UI doesn't refresh
// even though the value did change. Either way, a poll-and-diff reconciler
// catches it: if observed speed differs from what we last sent or applied,
// re-broadcast.
//
// Tick interval is the same as the silo reconciler (6 frames ~= 100ms);
// feeder count is the same order of magnitude as silos so the per-tick
// walk cost is negligible.
[RegisterTypeInIl2Cpp(false)]
public sealed class FeederSpeedReconciler : MonoBehaviour
{
    private const int TickEveryFrames = 6;

    private int _frameCounter;

    // plotId -> last-sent/applied speed. One feeder per plot, so plotId is
    // a sufficient key (no slot index like silos).
    private static readonly Dictionary<string, SlimeFeeder.FeedSpeed> _lastSent = new();

    public static void RecordState(string plotId, SlimeFeeder.FeedSpeed speed)
    {
        _lastSent[plotId] = speed;
    }

    private void Update()
    {
        if (!MultiplayerActive) return;
        if (++_frameCounter < TickEveryFrames) return;
        _frameCounter = 0;

        var landPlots = SceneContext.Instance?.GameModel?.landPlots;
        if (landPlots == null) return;

        foreach (var entry in landPlots)
        {
            var go = entry.value?.gameObj;
            if (!go) continue;

            var feeder = go.GetComponentInChildren<SlimeFeeder>();
            if (!feeder) continue;

            var loc = feeder.GetComponentInParent<LandPlotLocation>();
            if (loc == null) continue;

            var current = feeder.GetFeedingCycleSpeed();
            var hasPrev = _lastSent.TryGetValue(loc._id, out var prev);

            // Same as SiloReconciler: silently establish a baseline on first
            // observation so a freshly-loaded world doesn't broadcast its
            // default-Normal feeders over the host's actual values.
            if (!hasPrev)
            {
                _lastSent[loc._id] = current;
                continue;
            }

            if (prev == current) continue;

            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-Feeder] Reconcile diff plot={loc._id} speed={current} prev={prev}");

            _lastSent[loc._id] = current;

            Main.SendToAllOrServer(new FeederSpeedPacket
            {
                PlotID = loc._id,
                Speed = (byte)current,
            });
        }
    }
}
