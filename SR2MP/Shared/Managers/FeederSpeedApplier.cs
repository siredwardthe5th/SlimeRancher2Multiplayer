using SR2MP.Components.World;

namespace SR2MP.Shared.Managers;

// Shared receive logic so client and server handlers don't drift.
//
// SR2's SlimeFeeder has TWO independent paths to update speed:
//   - SetFeederSpeed(value)  : sets the underlying speed value
//   - StepFeederSpeed()      : advances to next value (UI button uses this)
// And a SEPARATE method to refresh the dial sprite:
//   - SetFeederSpeedIcon(value)
//
// In testing, calling SetFeederSpeed alone changed the feeding behavior on
// the receiver but the dial graphic stayed on its previous icon — so the
// remote player saw the speed-change "not sync" even though the simulation
// did update. We explicitly refresh the icon to keep the visible dial in
// sync with the value.
internal static class FeederSpeedApplier
{
    public static void Apply(SlimeFeeder feeder, SlimeFeeder.FeedSpeed speed, string plotId)
    {
        if (feeder == null) return;

        var before = feeder.GetFeedingCycleSpeed();

        handlingPacket = true;
        try
        {
            feeder.SetFeederSpeed(speed);
            feeder.SetFeederSpeedIcon(speed);
        }
        finally { handlingPacket = false; }

        // Seed the reconciler cache so the just-applied remote change isn't
        // observed as a local-side diff and bounced back.
        FeederSpeedReconciler.RecordState(plotId, speed);

        if (Main.DiagnosticLogging)
        {
            var after = feeder.GetFeedingCycleSpeed();
            SrLogger.LogMessage($"[SR2MP-Diag-Feeder] Apply plot={plotId} requested={speed} before={before} after={after}");
        }
    }
}
