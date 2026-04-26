using HarmonyLib;
using SR2MP.Packets.Landplot;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Plots;

// Re-enabled for 1.2.0 + multiplayer testing with verbose diagnostic logging.
// Upstream commits "Temporarily disabled garden synchronization" and
// "Temporarily disabled broken garden synchronization" disabled this patch
// because gardens were not syncing correctly. We re-enable in lockstep with
// OnDestroyCrop and OnResourceAttach so we can capture both sides of the
// garden lifecycle in MelonLogger output and find what was actually broken.
//
// Known smell: receive handler (GardenPlantHandler) treats ActorType == 9 as
// a "destroy" sentinel. If persistent ID 9 collides with a real plantable
// actor type, planting that actor would trigger an erroneous destroy on
// remote clients. Worth checking against the [SR2MP-Diag-FX]-style dump.
[HarmonyPatch(typeof(GardenCatcher), nameof(GardenCatcher.Plant))]
public static class PlantGarden
{
    public static void Postfix(GardenCatcher __instance, IdentifiableType cropId)
    {
        if (handlingPacket) return;
        if (!MultiplayerActive) return;

        var loc = __instance.GetComponentInParent<LandPlotLocation>();
        if (loc == null) return;

        var packet = new GardenPlantPacket
        {
            ActorType = NetworkActorManager.GetPersistentID(cropId),
            ID = loc._id,
        };

        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Garden] Plant -> sending plot={loc._id} crop={cropId?.name} typeId={packet.ActorType}");

        Main.SendToAllOrServer(packet);
    }
}
