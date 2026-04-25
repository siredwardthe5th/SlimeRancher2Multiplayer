using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.GadgetPlace)]
public sealed class GadgetPlaceHandler : BasePacketHandler<GadgetPlacePacket>
{
    public GadgetPlaceHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(GadgetPlacePacket packet, IPEndPoint senderEndPoint)
    {
        var actorId = new ActorId(packet.ActorId);
        var definition = actorManager.ActorTypes[packet.TypeId].Cast<GadgetDefinition>();
        var sceneGroup = NetworkSceneManager.GetSceneGroup(packet.SceneGroupId);

        var model = SceneContext.Instance.GameModel.CreateGadgetModel(definition, actorId, sceneGroup, packet.Position, false);
        model.eulerRotation = packet.EulerRotation;
        actorManager.Actors[packet.ActorId] = model;

        handlingPacket = true;
        GadgetDirector.InstantiateGadgetFromModel(model);
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}
