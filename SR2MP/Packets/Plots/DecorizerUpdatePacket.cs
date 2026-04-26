using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Plots;

public sealed class DecorizerUpdatePacket : IPacket
{
    public int TypeId { get; set; }
    public bool IsAdd { get; set; }

    public PacketType Type => PacketType.DecorizerUpdate;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteInt(TypeId);
        writer.WriteBool(IsAdd);
    }

    public void Deserialise(PacketReader reader)
    {
        TypeId = reader.ReadInt();
        IsAdd = reader.ReadBool();
    }
}
