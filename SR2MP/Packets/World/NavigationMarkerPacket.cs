using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public struct NavigationMarkerPacket : IPacket
{
    public bool IsSet { get; set; }
    public Vector3 Position { get; set; }
    public string MapName { get; set; }

    public readonly PacketType Type => PacketType.NavigationMarker;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteBool(IsSet);
        if (IsSet)
        {
            writer.WriteVector3(Position);
            writer.WriteString(MapName);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        IsSet = reader.ReadBool();
        if (IsSet)
        {
            Position = reader.ReadVector3();
            MapName = reader.ReadString();
        }
    }
}
