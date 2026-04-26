using System.Net;
using SR2MP.Packets.Landplot;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.GardenPlant)]
public sealed class GardenPlantHandler : BasePacketHandler<GardenPlantPacket>
{
    public GardenPlantHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(GardenPlantPacket packet, IPEndPoint clientEp)
    {
        GardenPlantApplier.Apply(packet);
        Main.Server.SendToAllExcept(packet, clientEp);
    }
}
