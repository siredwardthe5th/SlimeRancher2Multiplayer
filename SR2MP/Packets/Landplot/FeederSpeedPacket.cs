using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Landplot;

public sealed class FeederSpeedPacket : IPacket
{
    public string PlotID { get; set; } = string.Empty;
    public byte Speed { get; set; }   // SlimeFeeder.FeedSpeed enum value

    public PacketType Type => PacketType.FeederSpeed;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlotID);
        writer.WriteByte(Speed);
    }

    public void Deserialise(PacketReader reader)
    {
        PlotID = reader.ReadString();
        Speed = reader.ReadByte();
    }
}
