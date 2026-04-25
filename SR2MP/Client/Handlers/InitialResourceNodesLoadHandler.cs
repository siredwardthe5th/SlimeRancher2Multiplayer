using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialResourceNodes)]
public sealed class InitialResourceNodesLoadHandler : BaseClientPacketHandler<InitialResourceNodesPacket>
{
    public InitialResourceNodesLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(InitialResourceNodesPacket packet)
    {
        var allSpawners = Resources.FindObjectsOfTypeAll<ResourceNodeSpawner>();

        handlingPacket = true;
        foreach (var entry in packet.Nodes)
        {
            var spawner = allSpawners.FirstOrDefault(x => x.Id == entry.SpawnerId);
            if (spawner == null) continue;

            if (entry.IsSpawned && !spawner.HasAttachedNode)
                spawner.SpawnNode(spawner.ResourceNodeDefinitions[entry.VariantIndex]);
            else if (!entry.IsSpawned && spawner.HasAttachedNode)
                spawner.DespawnNode();
        }
        handlingPacket = false;
    }
}
