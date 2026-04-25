using HarmonyLib;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(SlimeEat), nameof(SlimeEat.MaybeChomp))]
public static class DiagMaybeChomp
{
    private static float _lastLog = -999f;

    public static void Postfix(bool __result, SlimeEat __instance, GameObject obj)
    {
        if (obj == null) return;
        var now = UnityEngine.Time.unscaledTime;
        if (now - _lastLog < 2f) return;
        _lastLog = now;
        SrLogger.LogMessage($"[SR2MP-Eat] MaybeChomp: slime='{__instance?.name}' food='{obj.name}' result={__result}");
    }
}

[HarmonyPatch(typeof(SlimeEat), nameof(SlimeEat.FinishChomp))]
public static class DiagFinishChomp
{
    public static void Prefix(SlimeEat __instance, GameObject chomping)
    {
        SrLogger.LogMessage($"[SR2MP-Eat] FinishChomp: slime='{__instance?.name}' food='{chomping?.name}'");
    }
}
