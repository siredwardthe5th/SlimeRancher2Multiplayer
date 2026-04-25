using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ResourceNodeUpdate)]
public sealed class ResourceNodeUpdateHandler : BaseClientPacketHandler<ResourceNodeUpdatePacket>
{
    public ResourceNodeUpdateHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(ResourceNodeUpdatePacket packet)
    {
        var spawner = Resources.FindObjectsOfTypeAll<ResourceNodeSpawner>()
            .FirstOrDefault(x => x.Id == packet.SpawnerId);
        if (spawner == null) return;

        handlingPacket = true;
        if (packet.IsSpawned)
            spawner.SpawnNode(spawner.ResourceNodeDefinitions[packet.VariantIndex]);
        else
            spawner.DespawnNode();
        handlingPacket = false;
    }
}
