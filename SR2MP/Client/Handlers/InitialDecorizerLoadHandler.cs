using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialDecorizer)]
public sealed class InitialDecorizerLoadHandler : BaseClientPacketHandler<InitialDecorizerPacket>
{
    public InitialDecorizerLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(InitialDecorizerPacket packet)
    {
        var model = SceneContext.Instance.GameModel.GetDecorizerModel();

        handlingPacket = true;
        foreach (var entry in packet.Contents)
        {
            var identType = actorManager.ActorTypes[entry.Key];
            for (var i = 0; i < entry.Value; i++)
                model.Add(identType);
        }
        handlingPacket = false;
    }
}
