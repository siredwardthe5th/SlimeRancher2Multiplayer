using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.World;

[HarmonyPatch(typeof(Gadget), nameof(Gadget.OnPlaced))]
public static class OnGadgetPlaced
{
    public static void Postfix(Gadget __instance)
    {
        if (handlingPacket) return;
        if (!MultiplayerActive) return;

        var model = __instance._model;
        if (model == null || model.actorId.Value == 0) return;

        actorManager.Actors[model.actorId.Value] = model;

        var packet = new GadgetPlacePacket
        {
            ActorId = model.actorId.Value,
            TypeId = NetworkActorManager.GetPersistentID(model.ident),
            Position = model.GetPos(),
            EulerRotation = model.eulerRotation,
            SceneGroupId = NetworkSceneManager.GetPersistentID(model.sceneGroup)
        };
        Main.SendToAllOrServer(packet);
    }
}
