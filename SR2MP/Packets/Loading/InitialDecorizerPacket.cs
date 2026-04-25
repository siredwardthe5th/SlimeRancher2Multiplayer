using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialDecorizerPacket : IPacket
{
    public Dictionary<int, int> Contents { get; set; } = new();

    public PacketType Type => PacketType.InitialDecorizer;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteDictionary(Contents, (w, k) => w.WriteInt(k), (w, v) => w.WriteInt(v));
    }

    public void Deserialise(PacketReader reader)
    {
        Contents = reader.ReadDictionary(r => r.ReadInt(), r => r.ReadInt());
    }
}
