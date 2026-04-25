using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ResourceNodeUpdate)]
public sealed class ResourceNodeUpdateHandler : BasePacketHandler<ResourceNodeUpdatePacket>
{
    public ResourceNodeUpdateHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(ResourceNodeUpdatePacket packet, IPEndPoint senderEndPoint)
    {
        var spawner = Resources.FindObjectsOfTypeAll<ResourceNodeSpawner>()
            .FirstOrDefault(x => x.Id == packet.SpawnerId);

        if (spawner != null)
        {
            handlingPacket = true;
            if (packet.IsSpawned)
                spawner.SpawnNode(spawner.ResourceNodeDefinitions[packet.VariantIndex]);
            else
                spawner.DespawnNode();
            handlingPacket = false;
        }

        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}
