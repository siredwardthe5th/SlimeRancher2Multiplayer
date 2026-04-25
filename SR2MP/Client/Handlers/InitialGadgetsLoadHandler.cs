using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialGadgets)]
public sealed class InitialGadgetsLoadHandler : BaseClientPacketHandler<InitialGadgetsPacket>
{
    public InitialGadgetsLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(InitialGadgetsPacket packet)
    {
        foreach (var entry in packet.Gadgets)
        {
            var actorId = new ActorId(entry.ActorId);
            var definition = actorManager.ActorTypes[entry.TypeId].Cast<GadgetDefinition>();
            var sceneGroup = NetworkSceneManager.GetSceneGroup(entry.SceneGroupId);

            var model = SceneContext.Instance.GameModel.CreateGadgetModel(definition, actorId, sceneGroup, entry.Position, false);
            model.eulerRotation = entry.EulerRotation;
            actorManager.Actors[entry.ActorId] = model;

            handlingPacket = true;
            GadgetDirector.InstantiateGadgetFromModel(model);
            handlingPacket = false;
        }
    }
}
