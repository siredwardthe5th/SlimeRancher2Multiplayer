using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.PlayerInventory)]
public sealed class PlayerInventoryHandler : BaseClientPacketHandler<PlayerInventoryPacket>
{
    public PlayerInventoryHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(PlayerInventoryPacket packet)
    {
        // Stubbed: RemotePlayer has no Inventory field in this source revision.
        // PacketType.PlayerInventory is registered but not wired to any RemotePlayer state.
        var player = playerManager.GetPlayer(packet.PlayerId);
        if (player == null) return;
        _ = packet.Slots;
    }
}
