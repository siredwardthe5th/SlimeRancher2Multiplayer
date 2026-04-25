using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.GadgetPlace)]
public sealed class GadgetPlaceHandler : BaseClientPacketHandler<GadgetPlacePacket>
{
    public GadgetPlaceHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(GadgetPlacePacket packet)
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
    }
}
