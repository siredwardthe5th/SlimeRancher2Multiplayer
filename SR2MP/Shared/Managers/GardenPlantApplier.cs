using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Landplot;

namespace SR2MP.Shared.Managers;

// Shared apply logic for garden plant/destroy packets. Logs the relevant
// state before and after the action so we can diagnose receive-side bugs
// without having to instrument both handlers.
//
// ActorType==9 is the destroy sentinel (matches OnDestroyCrop sender).
internal static class GardenPlantApplier
{
    public static void Apply(GardenPlantPacket packet)
    {
        var landPlots = SceneContext.Instance?.GameModel?.landPlots;
        if (landPlots == null || !landPlots.ContainsKey(packet.ID))
        {
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-Garden] Apply: plot {packet.ID} not in landPlots, dropping");
            return;
        }

        var model = landPlots[packet.ID];

        if (packet.ActorType == 9)
            ApplyDestroy(packet.ID, model);
        else
            ApplyPlant(packet.ID, model, packet.ActorType);
    }

    private static void ApplyDestroy(string plotId, LandPlotModel model)
    {
        model.resourceGrowerDefinition = null;

        if (!model.gameObj)
        {
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-Garden] Apply destroy plot={plotId}: gameObj null, skipped");
            return;
        }

        var plot = model.gameObj.GetComponentInChildren<LandPlot>();
        if (plot == null)
        {
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-Garden] Apply destroy plot={plotId}: no LandPlot component found, skipped");
            return;
        }

        var hadBefore = plot.HasAttached();
        var beforeCrop = hadBefore ? plot.GetAttachedCropId()?.name ?? "<null>" : "<none>";

        handlingPacket = true;
        try { plot.DestroyAttached(); }
        finally { handlingPacket = false; }

        if (Main.DiagnosticLogging)
        {
            var hasAfter = plot.HasAttached();
            SrLogger.LogMessage($"[SR2MP-Diag-Garden] Apply destroy plot={plotId} before(attached={hadBefore} crop={beforeCrop}) after(attached={hasAfter})");
        }
    }

    private static void ApplyPlant(string plotId, LandPlotModel model, int actorType)
    {
        if (!actorManager.ActorTypes.TryGetValue(actorType, out var actor))
        {
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-Garden] Apply plant plot={plotId}: unknown actorType={actorType}, dropping");
            return;
        }

        model.resourceGrowerDefinition =
            GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._resourceGrowerTranslation
                .RawLookupDictionary._entries.FirstOrDefault(x =>
                    x.value._primaryResourceType == actor)!.value;

        if (!model.gameObj)
        {
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-Garden] Apply plant plot={plotId} actor={actor?.name}: gameObj null, skipped");
            return;
        }

        var garden = model.gameObj.GetComponentInChildren<GardenCatcher>();
        if (garden == null)
        {
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-Garden] Apply plant plot={plotId} actor={actor?.name}: no GardenCatcher, skipped");
            return;
        }

        var plot = garden.Activator;
        var hadBefore = plot != null && plot.HasAttached();
        var beforeCrop = hadBefore ? plot.GetAttachedCropId()?.name ?? "<null>" : "<none>";
        var canAccept = garden.CanAccept(actor);

        GameObject? planted = null;
        handlingPacket = true;
        try
        {
            if (canAccept)
                planted = garden.Plant(actor, true);
        }
        finally { handlingPacket = false; }

        if (Main.DiagnosticLogging)
        {
            var hasAfter = plot != null && plot.HasAttached();
            SrLogger.LogMessage($"[SR2MP-Diag-Garden] Apply plant plot={plotId} actor={actor?.name} canAccept={canAccept} before(attached={hadBefore} crop={beforeCrop}) after(attached={hasAfter} planted={planted != null})");
        }
    }
}
