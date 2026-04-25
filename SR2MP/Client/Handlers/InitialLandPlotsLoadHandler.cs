using SR2MP.Packets.Loading;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialPlots)]
public sealed class PlotsLoadHandler : BaseClientPacketHandler<InitialLandPlotsPacket>
{
    public PlotsLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    private static void ApplySlotEntries(SiloStorage? storage, List<InitialLandPlotsPacket.SiloData.SlotEntry> entries)
    {
        if (!storage || entries == null) return;
        var ammo = storage.GetRelevantAmmo();
        if (ammo == null) return;
        handlingPacket = true;
        foreach (var entry in entries)
        {
            if (entry.SlotIndex < 0 || entry.SlotIndex >= ammo.Slots.Count) continue;
            var identType = actorManager.ActorTypes[entry.ActorTypeId];
            if (!identType) continue;
            ammo.Clear(entry.SlotIndex);
            ammo.MaybeAddResource(identType, entry.SlotIndex, entry.Count, false);
        }
        handlingPacket = false;
    }

    protected override void Handle(InitialLandPlotsPacket packet)
    {
        foreach (var plot in packet.Plots)
        {
            var model = SceneContext.Instance.GameModel.landPlots[plot.ID];

            if (model.gameObj)
            {
                handlingPacket = true;
                var location = model.gameObj.GetComponent<LandPlotLocation>();
                var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();
                location.Replace(landPlotComponent, GameContext.Instance.LookupDirector._plotPrefabDict[plot.Type]);

                var landPlotComponent2 = model.gameObj.GetComponentInChildren<LandPlot>();
                landPlotComponent2.ApplyUpgrades(plot.Upgrades.Cast<CppCollections.IEnumerable<LandPlot.Upgrade>>(), false);
                handlingPacket = false;
            }

            model.typeId = plot.Type;
            model.upgrades = plot.Upgrades;

            switch (plot.Data)
            {
                case InitialLandPlotsPacket.GardenData { Crop: 9 }:
                {
                    model.resourceGrowerDefinition = null;
                    if (!model.gameObj)
                        continue;
                    var gardenPlot = model.gameObj.GetComponentInChildren<LandPlot>();
                    gardenPlot.DestroyAttached();
                    break;
                }
                case InitialLandPlotsPacket.GardenData garden:
                {
                    var actor = actorManager.ActorTypes[garden.Crop];
                    model.resourceGrowerDefinition =
                        GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._resourceGrowerTranslation
                           .RawLookupDictionary._entries.FirstOrDefault(x =>
                                x.value._primaryResourceType == actor)!.value;

                    if (!model.gameObj)
                        continue;
                    var gardenCatcher = model.gameObj.GetComponentInChildren<GardenCatcher>();

                    if (gardenCatcher.CanAccept(actor))
                        gardenCatcher.Plant(actor, true);
                    break;
                }
                case InitialLandPlotsPacket.SiloData silo:
                {
                    if (!model.gameObj) break;
                    ApplySlotEntries(model.gameObj.GetComponentInChildren<SiloStorage>(), silo.Slots);
                    break;
                }
                case InitialLandPlotsPacket.CorralData corral:
                {
                    if (!model.gameObj) break;
                    ApplySlotEntries(model.gameObj.GetComponentInChildren<PlortCollector>()?._storage, corral.PlortSlots);
                    ApplySlotEntries(model.gameObj.GetComponentInChildren<SlimeFeeder>()?._storage, corral.FeederSlots);
                    break;
                }
            }
        }
    }
}