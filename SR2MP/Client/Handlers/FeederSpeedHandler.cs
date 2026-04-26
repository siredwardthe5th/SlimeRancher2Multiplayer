using SR2MP.Packets.Landplot;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.FeederSpeed)]
public sealed class FeederSpeedHandler : BaseClientPacketHandler<FeederSpeedPacket>
{
    public FeederSpeedHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(FeederSpeedPacket packet)
    {
        var landPlots = SceneContext.Instance?.GameModel?.landPlots;
        if (landPlots == null) return;
        if (!landPlots.ContainsKey(packet.PlotID)) return;

        var model = landPlots[packet.PlotID];
        if (!model.gameObj) return;

        var feeder = model.gameObj.GetComponentInChildren<SlimeFeeder>();
        if (!feeder) return;

        FeederSpeedApplier.Apply(feeder, (SlimeFeeder.FeedSpeed)packet.Speed, packet.PlotID);
    }
}
