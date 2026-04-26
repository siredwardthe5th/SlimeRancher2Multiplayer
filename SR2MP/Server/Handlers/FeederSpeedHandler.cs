using System.Net;
using SR2MP.Packets.Landplot;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.FeederSpeed)]
public sealed class FeederSpeedHandler : BasePacketHandler<FeederSpeedPacket>
{
    public FeederSpeedHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(FeederSpeedPacket packet, IPEndPoint senderEndPoint)
    {
        var landPlots = SceneContext.Instance?.GameModel?.landPlots;
        if (landPlots != null && landPlots.ContainsKey(packet.PlotID))
        {
            var model = landPlots[packet.PlotID];
            if (model.gameObj)
            {
                var feeder = model.gameObj.GetComponentInChildren<SlimeFeeder>();
                if (feeder)
                {
                    FeederSpeedApplier.Apply(feeder, (SlimeFeeder.FeedSpeed)packet.Speed, packet.PlotID);
                }
            }
        }

        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}
