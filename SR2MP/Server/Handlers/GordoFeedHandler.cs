using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Gordo;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.GordoFeed)]
public sealed class GordoFeedHandler : BasePacketHandler<GordoFeedPacket>
{
    public GordoFeedHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(GordoFeedPacket packet, IPEndPoint clientEp)
    {
        if (SceneContext.Instance.GameModel.gordos.TryGetValue(packet.ID, out var gordo))
        {
            gordo.GordoEatenCount = packet.NewFoodCount;
        }
        else
        {
            // FIX: Guard against unknown gordo types instead of throwing KeyNotFoundException
            // from actorManager.ActorTypes[packet.GordoType].
            if (!actorManager.ActorTypes.TryGetValue(packet.GordoType, out var gordoType))
            {
                SrLogger.LogWarning($"GordoFeed: unknown gordo type {packet.GordoType}", SrLogTarget.Both);
                Main.Server.SendToAllExcept(packet, clientEp);
                return;
            }

            gordo = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoEatCount = packet.NewFoodCount,
                gordoSeen = false,
                gameObj = null,
                targetCount = packet.RequiredFoodCount,
                identifiableType = gordoType
            };

            SceneContext.Instance.GameModel.gordos.Add(packet.ID, gordo);
        }

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}
