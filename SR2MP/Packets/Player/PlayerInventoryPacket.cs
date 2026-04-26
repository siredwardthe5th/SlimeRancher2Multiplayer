using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public sealed class PlayerInventoryPacket : IPacket
{
    public struct SlotData : INetObject
    {
        public int SlotIndex { get; set; }
        public int ActorTypeId { get; set; }
        public int Count { get; set; }
        public int MaxCount { get; set; }

        public readonly void Serialise(PacketWriter writer)
        {
            writer.WriteInt(SlotIndex);
            writer.WriteInt(ActorTypeId);
            writer.WriteInt(Count);
            writer.WriteInt(MaxCount);
        }

        public void Deserialise(PacketReader reader)
        {
            SlotIndex = reader.ReadInt();
            ActorTypeId = reader.ReadInt();
            Count = reader.ReadInt();
            MaxCount = reader.ReadInt();
        }
    }

    public string PlayerId { get; set; }
    public List<SlotData> Slots { get; set; }

    public PacketType Type => PacketType.PlayerInventory;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteList(Slots, PacketWriterDels.NetObject<SlotData>.Func);
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        Slots = reader.ReadList(PacketReaderDels.NetObject<SlotData>.Func);
    }
}
