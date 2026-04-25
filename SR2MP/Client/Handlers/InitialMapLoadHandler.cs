using Il2CppMonomiPark.SlimeRancher.Event;
using Il2CppMonomiPark.SlimeRancher.Map;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialMapEntries)]
public sealed class InitialMapLoadHandler : BaseClientPacketHandler<InitialMapPacket>
{
    public InitialMapLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(InitialMapPacket packet)
    {
        var eventModel = SceneContext.Instance.eventDirector._model;

        eventModel.table[MapEventKey] = new CppCollections.Dictionary<string, EventRecordModel.Entry>();

        foreach (var node in packet.UnlockedNodes)
        {
            eventModel.table[MapEventKey][node] = new EventRecordModel.Entry
            {
                count = 1,
                createdRealTime = 0,
                createdGameTime = 0,
                dataKey = node,
                eventKey = MapEventKey,
                updatedRealTime = 0,
                updatedGameTime = 0,
            };

            var gameEvent = Resources.FindObjectsOfTypeAll<StaticGameEvent>().FirstOrDefault(x => x._dataKey == node);
            if (gameEvent != null)
                SceneContext.Instance.MapDirector.NotifyZoneUnlocked(gameEvent, false, 0);
        }

        if (packet.HasNavMarker && !string.IsNullOrEmpty(packet.NavMarkerMapName))
        {
            var director = SceneContext.Instance.MapDirector;
            var allMaps = director._mapList._maps;
            MapDefinition mapDef = null;
            for (int i = 0; i < allMaps.Length; i++)
                if (allMaps[i].name == packet.NavMarkerMapName) { mapDef = allMaps[i]; break; }
            if (mapDef != null)
            {
                handlingPacket = true;
                director.SetPlayerNavigationMarker(packet.NavMarkerPosition, mapDef, 0f);
                handlingPacket = false;
            }
        }
    }
}