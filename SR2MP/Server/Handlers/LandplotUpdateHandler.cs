using System.Net;
using SR2MP.Packets.Landplot;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.LandPlotUpdate)]
public sealed class LandPlotUpdateHandler : BasePacketHandler<LandPlotUpdatePacket>
{
    public LandPlotUpdateHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(LandPlotUpdatePacket packet, IPEndPoint clientEp)
    {
        // FIX: Guard against unknown plot IDs instead of throwing KeyNotFoundException.
        if (!SceneContext.Instance.GameModel.landPlots.TryGetValue(packet.ID, out var model))
        {
            SrLogger.LogWarning($"LandPlotUpdate: plot {packet.ID} not found in game model", SrLogTarget.Both);
            return;
        }

        Main.Server.SendToAllExcept(packet, clientEp);

        if (!packet.IsUpgrade)
        {
            model.typeId = packet.PlotType;

            if (!model.gameObj) return;

            var location = model.gameObj.GetComponent<LandPlotLocation>();
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();

            location.Replace(landPlotComponent,
                GameContext.Instance.LookupDirector._plotPrefabDict[packet.PlotType]);
            return;
        }

        model.upgrades.Add(packet.PlotUpgrade);

        if (!model.gameObj) return;
        {
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();
            landPlotComponent.AddUpgrade(packet.PlotUpgrade);
        }
    }
}
