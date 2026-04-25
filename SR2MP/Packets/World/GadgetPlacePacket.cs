using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public sealed class GadgetPlacePacket : IPacket
{
    public long ActorId { get; set; }
    public int TypeId { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 EulerRotation { get; set; }
    public int SceneGroupId { get; set; }

    public PacketType Type => PacketType.GadgetPlace;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteLong(ActorId);
        writer.WriteInt(TypeId);
        writer.WriteVector3(Position);
        writer.WriteVector3(EulerRotation);
        writer.WriteInt(SceneGroupId);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = reader.ReadLong();
        TypeId = reader.ReadInt();
        Position = reader.ReadVector3();
        EulerRotation = reader.ReadVector3();
        SceneGroupId = reader.ReadInt();
    }
}
