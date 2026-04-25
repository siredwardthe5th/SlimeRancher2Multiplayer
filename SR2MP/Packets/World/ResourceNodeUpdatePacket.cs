using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public sealed class ResourceNodeUpdatePacket : IPacket
{
    public string SpawnerId { get; set; }
    public int VariantIndex { get; set; }
    public bool IsSpawned { get; set; }

    public PacketType Type => PacketType.ResourceNodeUpdate;
    public PacketReliability Reliability => PacketReliability.Reliable;

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
