using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialGadgetsPacket : IPacket
{
    public sealed class GadgetEntry : INetObject
    {
        public long ActorId { get; set; }
        public int TypeId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 EulerRotation { get; set; }
        public int SceneGroupId { get; set; }

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

    public List<GadgetEntry> Gadgets { get; set; } = new();

    public PacketType Type => PacketType.InitialGadgets;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
        => writer.WriteList(Gadgets, PacketWriterDels.NetObject<GadgetEntry>.Func);

    public void Deserialise(PacketReader reader)
        => Gadgets = reader.ReadList(PacketReaderDels.NetObject<GadgetEntry>.Func);
}
