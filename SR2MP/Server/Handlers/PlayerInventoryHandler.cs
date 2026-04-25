using System.Net;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerInventory)]
public sealed class PlayerInventoryHandler : BasePacketHandler<PlayerInventoryPacket>
{
    public PlayerInventoryHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(PlayerInventoryPacket packet, IPEndPoint clientEp)
    {
        Main.Server.SendToAllExcept(packet, clientEp);
    }
}
