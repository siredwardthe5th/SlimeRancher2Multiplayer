using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.World;

namespace SR2MP.Patches.World;

[HarmonyPatch(typeof(GameModel), nameof(GameModel.DestroyGadgetModel), typeof(GadgetModel))]
public static class OnGadgetRemoved
{
    public static void Prefix(GadgetModel model)
    {
        if (handlingPacket) return;
        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;
        if (model == null || model.actorId.Value == 0) return;
        if (!actorManager.Actors.ContainsKey(model.actorId.Value)) return;

        actorManager.Actors.Remove(model.actorId.Value);
        Main.SendToAllOrServer(new GadgetRemovePacket { ActorId = model.actorId.Value });
    }
}
