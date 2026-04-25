using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public sealed class GadgetRemovePacket : IPacket
{
    public long ActorId { get; set; }

    public PacketType Type => PacketType.GadgetRemove;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer) => writer.WriteLong(ActorId);
    public void Deserialise(PacketReader reader) => ActorId = reader.ReadLong();
}
