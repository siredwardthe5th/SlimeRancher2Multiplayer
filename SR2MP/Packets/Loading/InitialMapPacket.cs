using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialMapPacket : IPacket
{
    public List<string> UnlockedNodes { get; set; }
    public bool HasNavMarker { get; set; }
    public Vector3 NavMarkerPosition { get; set; }
    public string NavMarkerMapName { get; set; }

    public PacketType Type => PacketType.InitialMapEntries;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteList(UnlockedNodes, PacketWriterDels.String);
        writer.WriteBool(HasNavMarker);
        if (HasNavMarker)
        {
            writer.WriteVector3(NavMarkerPosition);
            writer.WriteString(NavMarkerMapName);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        UnlockedNodes = reader.ReadList(PacketReaderDels.String);
        HasNavMarker = reader.ReadBool();
        if (HasNavMarker)
        {
            NavMarkerPosition = reader.ReadVector3();
            NavMarkerMapName = reader.ReadString();
        }
    }
}