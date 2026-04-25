using System.Net;
using SR2MP.Client.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.NavigationMarker)]
public sealed class NavigationMarkerHandler : BasePacketHandler<NavigationMarkerPacket>
{
    public NavigationMarkerHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(NavigationMarkerPacket packet, IPEndPoint clientEp)
    {
        Main.Server.SendToAllExcept(packet, clientEp);
    }
}
