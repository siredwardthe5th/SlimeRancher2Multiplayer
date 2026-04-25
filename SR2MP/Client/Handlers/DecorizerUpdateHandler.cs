using SR2MP.Packets.Plots;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.DecorizerUpdate)]
public sealed class DecorizerUpdateHandler : BaseClientPacketHandler<DecorizerUpdatePacket>
{
    public DecorizerUpdateHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(DecorizerUpdatePacket packet)
    {
        var model = SceneContext.Instance.GameModel.GetDecorizerModel();
        var identType = actorManager.ActorTypes[packet.TypeId];

        handlingPacket = true;
        if (packet.IsAdd)
            model.Add(identType);
        else
            model.Remove(identType);
        handlingPacket = false;
    }
}
