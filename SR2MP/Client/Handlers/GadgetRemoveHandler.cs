using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.GadgetRemove)]
public sealed class GadgetRemoveHandler : BaseClientPacketHandler<GadgetRemovePacket>
{
    public GadgetRemoveHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(GadgetRemovePacket packet)
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
    }
}
