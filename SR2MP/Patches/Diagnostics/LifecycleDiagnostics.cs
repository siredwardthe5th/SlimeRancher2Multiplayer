using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.VFX.EnvironmentInteraction;
using Il2CppMonomiPark.SlimeRancher.World;

namespace SR2MP.Patches.Diagnostics;

// All patches in this file are gated on Main.DiagnosticLogging. They emit
// tagged [SR2MP-Diag-*] entries so we can grep MelonLogger output and learn
// the actual runtime behavior of game systems we want to sync. Toggle via
// MelonPreferences.cfg [SR2MP] diagnostic_logging = true and restart.
//
// All patches are Postfix-only and never modify return values, so leaving
// this enabled has no behavioral side effects beyond log output.

[HarmonyPatch(typeof(VacuumInteractionFX), nameof(VacuumInteractionFX.Start))]
internal static class DiagVacFXStart
{
    public static void Postfix(VacuumInteractionFX __instance)
    {
        if (!Main.DiagnosticLogging) return;
        var path = __instance.gameObject ? __instance.gameObject.GetGameObjectPath() : "<no-go>";
        SrLogger.LogMessage($"[SR2MP-Diag-VacFX] Start on '{__instance.name}' path='{path}'");
    }
}

[HarmonyPatch(typeof(VacuumInteractionFX), nameof(VacuumInteractionFX.Update))]
internal static class DiagVacFXUpdate
{
    private static int _ticksSinceLog;

    public static void Postfix(VacuumInteractionFX __instance)
    {
        if (!Main.DiagnosticLogging) return;
        // throttle to once every ~120 frames to avoid log floods
        if (++_ticksSinceLog < 120) return;
        _ticksSinceLog = 0;
        SrLogger.LogMessage($"[SR2MP-Diag-VacFX] Update tick on '{__instance.name}' (sampled every 120 frames)");
    }
}

[HarmonyPatch(typeof(EnvironmentInteractionVacuum), nameof(EnvironmentInteractionVacuum.SpawnVacuumRipple))]
internal static class DiagVacRipple
{
    public static void Postfix(EnvironmentInteractionVacuum __instance)
    {
        if (!Main.DiagnosticLogging) return;
        SrLogger.LogMessage($"[SR2MP-Diag-VacFX] SpawnVacuumRipple on '{__instance.name}'");
    }
}

[HarmonyPatch(typeof(SiloStorage), nameof(SiloStorage.MaybeAddAsResource))]
internal static class DiagSiloAddResource
{
    public static void Postfix(SiloStorage __instance, IdentifiableType id, int slotIdx, int count, bool overflow, bool __result)
    {
        if (!Main.DiagnosticLogging) return;
        var plotId = TryGetPlotId(__instance.gameObject);
        SrLogger.LogMessage($"[SR2MP-Diag-Silo] MaybeAddAsResource plot={plotId} slot={slotIdx} id={id?.name} count={count} overflow={overflow} result={__result}");
    }

    internal static string TryGetPlotId(GameObject go)
    {
        try
        {
            var loc = go.GetComponentInParent<LandPlotLocation>();
            return loc != null ? loc._id : "<no-loc>";
        }
        catch { return "<error>"; }
    }
}

// Disabled: SiloStorage.OnIdentifiableRemoved fires during MelonLoader's
// early IL2CPP type registration phase, before Main is initialized. Even
// though this Postfix is gated on Main.DiagnosticLogging (now null-safe),
// the mere act of installing the Harmony patch on this method appears to
// hard-crash the process during early init (process termination with no
// managed exception, log stops at "Registered mono icall ... SetAsLastSibling").
// Same root cause as Patches/Plots/OnSiloContentChanged.cs OnSiloRemove.
/*
[HarmonyPatch(typeof(SiloStorage), nameof(SiloStorage.OnIdentifiableRemoved))]
internal static class DiagSiloRemove
{
    public static void Postfix(SiloStorage __instance, IdentifiableType id)
    {
        if (!Main.DiagnosticLogging) return;
        var plotId = DiagSiloAddResource.TryGetPlotId(__instance.gameObject);
        SrLogger.LogMessage($"[SR2MP-Diag-Silo] OnIdentifiableRemoved plot={plotId} id={id?.name}");
    }
}
*/

[HarmonyPatch(typeof(PlortCollector), nameof(PlortCollector.DoCollection))]
internal static class DiagPlortCollect
{
    public static void Postfix(PlortCollector __instance)
    {
        if (!Main.DiagnosticLogging) return;
        var plotId = DiagSiloAddResource.TryGetPlotId(__instance.gameObject);
        SrLogger.LogMessage($"[SR2MP-Diag-Collector] DoCollection plot={plotId}");
    }
}

[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.EjectFood))]
internal static class DiagFeederEject
{
    public static void Postfix(SlimeFeeder __instance)
    {
        if (!Main.DiagnosticLogging) return;
        var plotId = DiagSiloAddResource.TryGetPlotId(__instance.gameObject);
        SrLogger.LogMessage($"[SR2MP-Diag-Feeder] EjectFood plot={plotId} foodId={__instance.GetFoodId()?.name} count={__instance.GetFoodCount()}");
    }
}

[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.SetFeederSpeed))]
internal static class DiagFeederSpeed
{
    public static void Postfix(SlimeFeeder __instance, SlimeFeeder.FeedSpeed speed)
    {
        if (!Main.DiagnosticLogging) return;
        var plotId = DiagSiloAddResource.TryGetPlotId(__instance.gameObject);
        SrLogger.LogMessage($"[SR2MP-Diag-Feeder] SetFeederSpeed plot={plotId} speed={speed}");
    }
}

[HarmonyPatch(typeof(GardenCatcher), nameof(GardenCatcher.Plant))]
internal static class DiagGardenPlant
{
    public static void Postfix(GardenCatcher __instance, IdentifiableType cropId)
    {
        if (!Main.DiagnosticLogging) return;
        var plotId = DiagSiloAddResource.TryGetPlotId(__instance.gameObject);
        SrLogger.LogMessage($"[SR2MP-Diag-Garden] Plant plot={plotId} crop={cropId?.name}");
    }
}

[HarmonyPatch(typeof(LightningStrike), nameof(LightningStrike.Start))]
internal static class DiagLightningStart
{
    public static void Postfix(LightningStrike __instance)
    {
        if (!Main.DiagnosticLogging) return;
        var isNet = __instance.gameObject.name.Contains("net", StringComparison.InvariantCultureIgnoreCase);
        SrLogger.LogMessage($"[SR2MP-Diag-Lightning] Start name='{__instance.name}' pos={__instance.transform.position} isNet={isNet}");
    }
}
