using SR2MP.Packets.Landplot;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.SiloContent)]
public sealed class SiloContentHandler : BaseClientPacketHandler<SiloContentPacket>
{
    public SiloContentHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(SiloContentPacket packet)
    {
        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Silo] Client applying seq={packet.Sequence} plot={packet.PlotID} slot={packet.SlotIndex} typeId={packet.ActorTypeId} count={packet.Count}");

        SiloContentApplier.Apply(packet);
    }
}
