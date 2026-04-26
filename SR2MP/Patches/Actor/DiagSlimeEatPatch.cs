using HarmonyLib;

namespace SR2MP.Patches.Actor;

// `?.name` on Il2Cpp wrappers checks the C# wrapper but not the underlying
// Il2Cpp object. If the GameObject was destroyed between the patch firing
// and the name lookup, UnityEngine.Object.get_name throws NRE inside il2cpp.
// Catch broadly so the diagnostic never propagates an exception out of a
// hot game-loop patch.
internal static class DiagEatHelpers
{
    public static string SafeName(Object obj)
    {
        if (obj == null) return "<null>";
        try { return obj.name; }
        catch { return "<destroyed>"; }
    }
}

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
        SrLogger.LogMessage($"[SR2MP-Eat] MaybeChomp: slime='{DiagEatHelpers.SafeName(__instance)}' food='{DiagEatHelpers.SafeName(obj)}' result={__result}");
    }
}

[HarmonyPatch(typeof(SlimeEat), nameof(SlimeEat.FinishChomp))]
public static class DiagFinishChomp
{
    public static void Prefix(SlimeEat __instance, GameObject chomping)
    {
        SrLogger.LogMessage($"[SR2MP-Eat] FinishChomp: slime='{DiagEatHelpers.SafeName(__instance)}' food='{DiagEatHelpers.SafeName(chomping)}'");
    }
}
