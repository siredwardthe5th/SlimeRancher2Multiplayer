using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialLandPlotsPacket : IPacket
{
    public sealed class BasePlot : INetObject
    {
        private static readonly Dictionary<LandPlot.Id, Type> DataTypes = new()
        {
            { LandPlot.Id.GARDEN,  typeof(GardenData)  },
            { LandPlot.Id.SILO,    typeof(SiloData)    },
            { LandPlot.Id.CORRAL,  typeof(CorralData)  },
        };

        public string ID { get; set; }
        public LandPlot.Id  Type { get; set; }
        public CppCollections.HashSet<LandPlot.Upgrade> Upgrades { get; set; }

        public INetObject? Data { get; set; }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteEnum(Type);
            writer.WriteCppSet(Upgrades, PacketWriterDels.Enum<LandPlot.Upgrade>.Func);

            Data?.Serialise(writer);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadString();
            Type = reader.ReadEnum<LandPlot.Id>();
            Upgrades = reader.ReadCppSet(PacketReaderDels.Enum<LandPlot.Upgrade>.Func);

            if (!DataTypes.TryGetValue(Type, out var dataType))
                return;

            Data = (INetObject)Activator.CreateInstance(dataType)!;
            Data.Deserialise(reader);
        }
    }

    public struct GardenData : INetObject
    {
        public int Crop { get; set; }

        public readonly void Serialise(PacketWriter writer) => writer.WriteInt(Crop);

        public void Deserialise(PacketReader reader) => Crop = reader.ReadInt();
    }

    public struct SiloData : INetObject
    {
        public struct SlotEntry : INetObject
        {
            public int SlotIndex { get; set; }
            public int ActorTypeId { get; set; }
            public int Count { get; set; }

            public readonly void Serialise(PacketWriter writer)
            {
                writer.WriteInt(SlotIndex);
                writer.WriteInt(ActorTypeId);
                writer.WriteInt(Count);
            }

            public void Deserialise(PacketReader reader)
            {
                SlotIndex = reader.ReadInt();
                ActorTypeId = reader.ReadInt();
                Count = reader.ReadInt();
            }
        }

        public List<SlotEntry> Slots { get; set; }

        public readonly void Serialise(PacketWriter writer)
            => writer.WriteList(Slots, PacketWriterDels.NetObject<SlotEntry>.Func);

        public void Deserialise(PacketReader reader)
            => Slots = reader.ReadList(PacketReaderDels.NetObject<SlotEntry>.Func);
    }

    public struct CorralData : INetObject
    {
        public List<SiloData.SlotEntry> PlortSlots { get; set; }
        public List<SiloData.SlotEntry> FeederSlots { get; set; }

        public readonly void Serialise(PacketWriter writer)
        {
            writer.WriteList(PlortSlots, PacketWriterDels.NetObject<SiloData.SlotEntry>.Func);
            writer.WriteList(FeederSlots, PacketWriterDels.NetObject<SiloData.SlotEntry>.Func);
        }

        public void Deserialise(PacketReader reader)
        {
            PlortSlots = reader.ReadList(PacketReaderDels.NetObject<SiloData.SlotEntry>.Func);
            FeederSlots = reader.ReadList(PacketReaderDels.NetObject<SiloData.SlotEntry>.Func);
        }
    }

    public List<BasePlot> Plots { get; set; }

    public PacketType Type => PacketType.InitialPlots;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer) => writer.WriteList(Plots, PacketWriterDels.NetObject<BasePlot>.Func);

    public void Deserialise(PacketReader reader) => Plots = reader.ReadList(PacketReaderDels.NetObject<BasePlot>.Func);
}