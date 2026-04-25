using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialResourceNodesPacket : IPacket
{
    public sealed class NodeEntry : INetObject
    {
        public string SpawnerId { get; set; }
        public int VariantIndex { get; set; }
        public bool IsSpawned { get; set; }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(SpawnerId);
            writer.WriteInt(VariantIndex);
            writer.WriteBool(IsSpawned);
        }

        public void Deserialise(PacketReader reader)
        {
            SpawnerId = reader.ReadString();
            VariantIndex = reader.ReadInt();
            IsSpawned = reader.ReadBool();
        }
    }

    public List<NodeEntry> Nodes { get; set; } = new();

    public PacketType Type => PacketType.InitialResourceNodes;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
        => writer.WriteList(Nodes, PacketWriterDels.NetObject<NodeEntry>.Func);

    public void Deserialise(PacketReader reader)
        => Nodes = reader.ReadList(PacketReaderDels.NetObject<NodeEntry>.Func);
}
