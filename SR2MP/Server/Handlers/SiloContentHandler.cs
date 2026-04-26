using System.Net;
using SR2MP.Packets.Landplot;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.SiloContent)]
public sealed class SiloContentHandler : BasePacketHandler<SiloContentPacket>
{
    public SiloContentHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(SiloContentPacket packet, IPEndPoint senderEndPoint)
    {
        // Trust the sender's reported value. We tried server-authority
        // (re-broadcasting host's slot state instead of applying the
        // client's), but the host has no local equivalent event for
        // client-initiated removes — the food actor that the client takes
        // out exists only on the client side until broadcast, so the host's
        // slot count stays pre-removal. Re-broadcasting that value
        // overwrote the client's correct local count and produced infinite
        // free food.
        //
        // Apply the packet on host (so host's display + storage updates),
        // and forward to other clients.
        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Silo] Server applying client packet seq={packet.Sequence} from={senderEndPoint} plot={packet.PlotID} slot={packet.SlotIndex} typeId={packet.ActorTypeId} count={packet.Count}");

        SiloContentApplier.Apply(packet);
        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}
