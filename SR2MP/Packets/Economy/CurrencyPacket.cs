using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Economy;

public struct CurrencyPacket : IPacket
{
    public int NewAmount { get; set; }
    public byte CurrencyType { get; set; }
    public bool ShowUINotification { get; set; }

    public readonly PacketType Type => PacketType.CurrencyAdjust;
    // ReliableOrdered: NewAmount is absolute, so out-of-order delivery
    // produces "every other sell skipped" visible drift on the receiver.
    public readonly PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteInt(NewAmount);
        writer.WriteByte(CurrencyType);
        writer.WriteBool(ShowUINotification);
    }

    public void Deserialise(PacketReader reader)
    {
        NewAmount = reader.ReadInt();
        CurrencyType = reader.ReadByte();
        ShowUINotification = reader.ReadBool();
    }
}