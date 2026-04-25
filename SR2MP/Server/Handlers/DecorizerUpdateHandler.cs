using System.Net;
using SR2MP.Packets.Plots;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.DecorizerUpdate)]
public sealed class DecorizerUpdateHandler : BasePacketHandler<DecorizerUpdatePacket>
{
    public DecorizerUpdateHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(DecorizerUpdatePacket packet, IPEndPoint senderEndPoint)
    {
        var model = SceneContext.Instance.GameModel.GetDecorizerModel();
        var identType = actorManager.ActorTypes[packet.TypeId];

        handlingPacket = true;
        if (packet.IsAdd)
            model.Add(identType);
        else
            model.Remove(identType);
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}
