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
        // The host echoes our own saved inventory back to us on connect.
        // Packets for other player IDs aren't relevant to us — there's no
        // UI for displaying a remote player's inventory, and the host
        // doesn't even forward those (this is a defense-in-depth check).
        if (packet.PlayerId != LocalID) return;

        var ps = SceneContext.Instance?.PlayerState;
        if (ps == null) return;

        var ammo = ps.Ammo;
        if (ammo == null) return;

        var slots = ammo.Slots;
        if (slots == null) return;

        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Inv] Applying restored inventory for self ({packet.Slots?.Count ?? 0} slots)");

        handlingPacket = true;
        try
        {
            if (packet.Slots == null) return;

            foreach (var slotData in packet.Slots)
            {
                if (slotData.SlotIndex < 0 || slotData.SlotIndex >= slots.Length) continue;
                var slot = slots[slotData.SlotIndex];
                if (slot == null) continue;

                if (slotData.ActorTypeId < 0 || slotData.Count <= 0)
                {
                    slot.Clear();
                }
                else if (actorManager.ActorTypes.TryGetValue(slotData.ActorTypeId, out var ident))
                {
                    slot.Id = ident;
                    slot.Count = slotData.Count;
                }
                // else: unknown actor type id — silently skip (logged at sender)
            }
        }
        finally { handlingPacket = false; }
    }
}
