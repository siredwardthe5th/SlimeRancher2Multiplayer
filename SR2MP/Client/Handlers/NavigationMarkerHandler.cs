using Il2CppMonomiPark.SlimeRancher.Map;
using SR2MP.Client.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.NavigationMarker)]
public sealed class NavigationMarkerHandler : BaseClientPacketHandler<NavigationMarkerPacket>
{
    public NavigationMarkerHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(NavigationMarkerPacket packet)
    {
        var director = SceneContext.Instance.MapDirector;
        handlingPacket = true;
        if (packet.IsSet)
        {
            var maps = director._mapList._maps;
            MapDefinition mapDef = null;
            for (int i = 0; i < maps.Length; i++)
                if (maps[i].name == packet.MapName) { mapDef = maps[i]; break; }
            if (mapDef != null)
                director.SetPlayerNavigationMarker(packet.Position, mapDef, 0f);
        }
        else
        {
            director.ClearPlayerNavigationMarker();
        }
        handlingPacket = false;
    }
}
