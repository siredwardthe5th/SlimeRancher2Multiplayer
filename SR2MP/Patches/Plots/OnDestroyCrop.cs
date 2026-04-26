using HarmonyLib;
using SR2MP.Packets.Landplot;

namespace SR2MP.Patches.Plots;

// Re-enabled for 1.2.0 + multiplayer testing. See PlantGarden.cs for context.
//
// LandPlot.DestroyAttached fires for every land plot type when its attached
// content is removed (garden harvested, silo cleared, etc.) — this patch
// intentionally only sends the destroy packet for plots that have a
// GardenCatcher, since GardenPlantHandler is the only receive path that
// understands the magic ActorType=9 sentinel. Without this filter we'd
// blast destroy packets for silo/feeder/decorizer destroys too.
[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.DestroyAttached))]
public static class OnDestroyCrop
{
    public static void Postfix(LandPlot __instance)
    {
        if (handlingPacket) return;
        if (!MultiplayerActive) return;
        if (!__instance) return;

        // Only forward garden-style destroys.
        if (!__instance.GetComponentInChildren<GardenCatcher>()) return;

        var loc = __instance.GetComponentInParent<LandPlotLocation>();
        if (loc == null) return;

        var packet = new GardenPlantPacket
        {
            ID = loc._id,
            ActorType = 9,   // sentinel: receive handler treats this as "destroy attached"
        };

        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Garden] DestroyAttached -> sending plot={loc._id} (garden)");

        Main.SendToAllOrServer(packet);
    }
}
