using SR2MP.Client.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.TornadoSpawn)]
public sealed class TornadoSpawnHandler : BaseClientPacketHandler<TornadoSpawnPacket>
{
    public TornadoSpawnHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(TornadoSpawnPacket packet)
    {
        var spawner = Resources.FindObjectsOfTypeAll<StaticTornadoSpawner>().FirstOrDefault(s => s._prefab);
        if (spawner == null) return;
        handlingPacket = true;
        spawner.Spawn(packet.Position, packet.Rotation);
        handlingPacket = false;
    }
}
