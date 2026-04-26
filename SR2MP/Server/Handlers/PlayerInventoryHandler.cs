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
        // Persist the inventory snapshot. Other clients have no UI for a
        // remote player's inventory, so don't forward — the host is the
        // sole consumer of this packet.
        PlayerInventoryStore.Save(packet.PlayerId, packet);

        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Inv] Stored inventory for player={packet.PlayerId} slots={packet.Slots?.Count ?? 0}");
    }
}
