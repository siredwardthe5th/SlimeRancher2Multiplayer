using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.GadgetRemove)]
public sealed class GadgetRemoveHandler : BasePacketHandler<GadgetRemovePacket>
{
    public GadgetRemoveHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(GadgetRemovePacket packet, IPEndPoint senderEndPoint)
    {
        if (!actorManager.Actors.TryGetValue(packet.ActorId, out var identModel)) return;
        var model = identModel.TryCast<GadgetModel>();
        if (model == null) return;

        var go = model.GetGameObject();
        actorManager.Actors.Remove(packet.ActorId);

        handlingPacket = true;
        SceneContext.Instance.GameModel.DestroyGadgetModel(model);
        handlingPacket = false;

        if (go) Object.Destroy(go);

        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}
