using HarmonyLib;
using SR2MP.Components.World;
using SR2MP.Packets.Landplot;

namespace SR2MP.Patches.Plots;

// Auto-feeder speed sync. Two patches because the UI button cycles speed
// via SlimeFeeder.StepFeederSpeed() — that path doesn't go through
// SetFeederSpeed, so without the StepFeederSpeed Postfix the in-game speed
// dial silently de-syncs. SetFeederSpeed remains patched because programmatic
// callers (load/save, mod APIs) use it directly. After either fires we read
// the resulting speed via GetFeedingCycleSpeed and broadcast it.

[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.SetFeederSpeed))]
public static class OnFeederSetSpeed
{
    public static void Postfix(SlimeFeeder __instance, SlimeFeeder.FeedSpeed speed)
        => FeederSpeedBroadcaster.Broadcast(__instance, speed);
}

[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.StepFeederSpeed))]
public static class OnFeederStepSpeed
{
    public static void Postfix(SlimeFeeder __instance)
        => FeederSpeedBroadcaster.Broadcast(__instance, __instance.GetFeedingCycleSpeed());
}

internal static class FeederSpeedBroadcaster
{
    public static void Broadcast(SlimeFeeder feeder, SlimeFeeder.FeedSpeed speed)
    {
        if (handlingPacket) return;
        if (!MultiplayerActive) return;
        if (feeder == null) return;

        var loc = feeder.GetComponentInParent<LandPlotLocation>();
        if (loc == null) return;

        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Feeder] Broadcast speed change plot={loc._id} speed={speed}");

        // Seed the reconciler cache before sending so its next tick sees no
        // diff and doesn't double-broadcast.
        FeederSpeedReconciler.RecordState(loc._id, speed);

        Main.SendToAllOrServer(new FeederSpeedPacket
        {
            PlotID = loc._id,
            Speed = (byte)speed,
        });
    }
}
