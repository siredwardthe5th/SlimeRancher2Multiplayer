using SR2MP.Components.World;
using SR2MP.Packets.Landplot;

namespace SR2MP.Shared.Managers;

// Shared apply logic so client and server handlers don't duplicate it.
internal static class SiloContentApplier
{
    public static void Apply(SiloContentPacket packet)
    {
        var landPlots = SceneContext.Instance?.GameModel?.landPlots;
        if (landPlots == null || !landPlots.ContainsKey(packet.PlotID)) return;

        var model = landPlots[packet.PlotID];
        if (!model.gameObj) return;

        var silo = model.gameObj.GetComponentInChildren<SiloStorage>();
        if (!silo) return;

        var ammo = silo.GetRelevantAmmo();
        if (ammo == null) return;
        var slots = ammo.Slots;
        if (slots == null || packet.SlotIndex < 0 || packet.SlotIndex >= slots.Length) return;

        var slot = slots[packet.SlotIndex];
        if (slot == null) return;

        handlingPacket = true;
        try
        {
            if (packet.Count <= 0 || packet.ActorTypeId < 0)
            {
                slot.Clear();
            }
            else if (actorManager.ActorTypes.TryGetValue(packet.ActorTypeId, out var ident))
            {
                slot.Id = ident;
                slot.Count = packet.Count;
            }
            // else: unknown actor type id — silently drop (logged at sender if diagnostics on)
        }
        finally { handlingPacket = false; }

        // Record the applied state so the reconciler doesn't see this remote
        // change as a local diff and bounce it back to the sender.
        SiloReconciler.RecordState(packet.PlotID, packet.SlotIndex, packet.ActorTypeId, packet.Count);
    }
}
