using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Map;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Map;

[HarmonyPatch(typeof(MapDirector), nameof(MapDirector.SetPlayerNavigationMarker))]
public static class OnNavigationMarkerSet
{
    public static void Postfix(bool __result, Vector3 position, MapDefinition onMap)
    {
        if (handlingPacket || !__result) return;
        Main.SendToAllOrServer(new NavigationMarkerPacket { IsSet = true, Position = position, MapName = onMap.name });
    }
}

[HarmonyPatch(typeof(MapDirector), nameof(MapDirector.ClearPlayerNavigationMarker))]
public static class OnNavigationMarkerClear
{
    public static void Postfix()
    {
        if (handlingPacket) return;
        Main.SendToAllOrServer(new NavigationMarkerPacket { IsSet = false });
    }
}
