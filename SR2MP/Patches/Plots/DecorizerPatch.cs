using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Plots;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Plots;

[HarmonyPatch(typeof(DecorizerModel), nameof(DecorizerModel.Add))]
public static class OnDecorizerAdd
{
    public static void Postfix(DecorizerModel __instance, IdentifiableType id, bool __result)
    {
        if (!__result) return;
        if (handlingPacket) return;

        var packet = new DecorizerUpdatePacket
        {
            TypeId = NetworkActorManager.GetPersistentID(id),
            IsAdd = true
        };
        Main.SendToAllOrServer(packet);
    }
}

[HarmonyPatch(typeof(DecorizerModel), nameof(DecorizerModel.Remove))]
public static class OnDecorizerRemove
{
    public static void Postfix(DecorizerModel __instance, IdentifiableType id, bool __result)
    {
        if (!__result) return;
        if (handlingPacket) return;

        var packet = new DecorizerUpdatePacket
        {
            TypeId = NetworkActorManager.GetPersistentID(id),
            IsAdd = false
        };
        Main.SendToAllOrServer(packet);
    }
}
