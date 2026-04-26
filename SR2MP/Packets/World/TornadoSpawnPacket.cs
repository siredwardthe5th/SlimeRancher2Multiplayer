using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public struct TornadoSpawnPacket : IPacket
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    public readonly PacketType Type => PacketType.TornadoSpawn;
    public readonly PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
    }

    public void Deserialise(PacketReader reader)
    {
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
    }
}
