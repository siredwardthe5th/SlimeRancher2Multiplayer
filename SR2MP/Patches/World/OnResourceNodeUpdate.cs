using HarmonyLib;
using SR2MP.Packets.World;

namespace SR2MP.Patches.World;

[HarmonyPatch(typeof(ResourceNodeSpawner), nameof(ResourceNodeSpawner.SpawnNode))]
public static class OnResourceNodeSpawned
{
    public static void Postfix(ResourceNodeSpawner __instance)
    {
        if (handlingPacket) return;

        var packet = new ResourceNodeUpdatePacket
        {
            SpawnerId = __instance.Id,
            VariantIndex = __instance._model.resourceNodeVariantIndex,
            IsSpawned = true
        };
        Main.SendToAllOrServer(packet);
    }
}

[HarmonyPatch(typeof(ResourceNodeSpawner), nameof(ResourceNodeSpawner.DespawnNode))]
public static class OnResourceNodeDespawned
{
    public static void Prefix(ResourceNodeSpawner __instance)
    {
        if (handlingPacket) return;

        var packet = new ResourceNodeUpdatePacket
        {
            SpawnerId = __instance.Id,
            VariantIndex = 0,
            IsSpawned = false
        };
        Main.SendToAllOrServer(packet);
    }
}
