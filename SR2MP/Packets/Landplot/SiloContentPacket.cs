using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Landplot;

// Reports the new state of a single silo slot after an add/remove. Sender
// captures the post-change snapshot from SiloStorage.GetSlot* and the receiver
// applies the absolute count rather than a delta — simpler and avoids drift
// from missed packets.
public sealed class SiloContentPacket : IPacket
{
    public string PlotID { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
    public int ActorTypeId { get; set; }   // -1 if slot is empty
    public int Count { get; set; }

    // Per-sender monotonic counter stamped by SiloBroadcaster.SendOne. Lets
    // us correlate send/apply log lines across machines without depending on
    // wall-clock alignment (clock skew between hosts can be >10s and
    // confounds timestamp-based log diffing). Wraps at uint.MaxValue, which
    // is fine for diagnostic correlation within a session.
    public uint Sequence { get; set; }

    public PacketType Type => PacketType.SiloContent;
    // ReliableOrdered, not Reliable: this packet carries an absolute Count
    // value, so a late-arriving packet from an earlier slot state would
    // overwrite a more recent state. Out-of-order delivery was producing
    // visible "every other update is wrong" inconsistency in testing.
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlotID);
        writer.WriteInt(SlotIndex);
        writer.WriteInt(ActorTypeId);
        writer.WriteInt(Count);
        writer.WriteUInt(Sequence);
    }

    public void Deserialise(PacketReader reader)
    {
        PlotID = reader.ReadString();
        SlotIndex = reader.ReadInt();
        ActorTypeId = reader.ReadInt();
        Count = reader.ReadInt();
        Sequence = reader.ReadUInt();
    }
}
