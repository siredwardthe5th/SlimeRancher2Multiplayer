using MelonLoader;
using SR2MP.Patches.Plots;
using SR2MP.Shared.Managers;

namespace SR2MP.Components.World;

// Periodically walks every silo on every land plot and broadcasts a
// SiloContentPacket for any slot whose (typeId, count) differs from the last
// value we sent or received. This is a robust safety net for content changes
// that don't go through any of our patched methods.
//
// History — earlier we tried to catch every silo content mutation via
// Harmony patches on AmmoSlot.set_Count, AmmoSlot.Clear, AmmoSlot.DecrementAmmo,
// AmmoSlotManager.Decrement, SiloCatcher.Remove, and SiloStorage.MaybeAdd*.
// Diagnostic logging proved that during fast vac-out drains, NONE of the
// remove-side patches caught all the mutations: a silo would visibly empty
// from 4 → 0 with at most one DecrementAmmo Postfix firing. The drain code
// path goes through some lower-level mutation we couldn't identify (and
// patching the property setter would be wide-reaching).
//
// The reconciler doesn't care HOW the count changed — only that the observed
// state diverges from the cache, which is updated on every send AND on every
// remote apply. Tick rate is 6 frames (~100ms at 60fps) — invisible for
// silo UI, low enough cost to walk a handful of plots.
[RegisterTypeInIl2Cpp(false)]
public sealed class SiloReconciler : MonoBehaviour
{
    private const int TickEveryFrames = 6;

    private int _frameCounter;

    // (plotId, slotIdx) -> (typeId, count). Updated by SiloBroadcaster.SendOne
    // when we send, and by SiloContentApplier.Apply when we receive a remote
    // packet. The latter prevents the reconciler from interpreting an applied
    // remote change as a local-only diff and bouncing it back.
    private static readonly Dictionary<(string, int), (int, int)> _lastSent = new();

    public static void RecordState(string plotId, int slotIdx, int typeId, int count)
    {
        _lastSent[(plotId, slotIdx)] = (typeId, count);
    }

    public static void Forget(string plotId)
    {
        // Drop entries for a plot that's been replaced/destroyed so its
        // stale cache doesn't suppress a real broadcast on the new plot.
        var keys = new List<(string, int)>();
        foreach (var k in _lastSent.Keys)
            if (k.Item1 == plotId)
                keys.Add(k);
        foreach (var k in keys)
            _lastSent.Remove(k);
    }

    private void Update()
    {
        if (!MultiplayerActive) return;
        if (++_frameCounter < TickEveryFrames) return;
        _frameCounter = 0;

        var landPlots = SceneContext.Instance?.GameModel?.landPlots;
        if (landPlots == null) return;

        foreach (var entry in landPlots)
        {
            var go = entry.value?.gameObj;
            if (!go) continue;

            var silo = go.GetComponentInChildren<SiloStorage>();
            if (!silo) continue;

            var loc = silo.GetComponentInParent<LandPlotLocation>();
            if (loc == null) continue;

            var ammo = silo.GetRelevantAmmo();
            if (ammo == null) continue;

            var slots = ammo.Slots;
            if (slots == null) continue;

            for (int i = 0; i < slots.Length; i++)
            {
                var ident = silo.GetSlotIdentifiable(i);
                var typeId = ident != null ? NetworkActorManager.GetPersistentID(ident) : -1;
                var count = silo.GetSlotCount(i);

                var key = (loc._id, i);
                var hasPrev = _lastSent.TryGetValue(key, out var prev);

                // First observation: silently establish the baseline. Without
                // this, a freshly-joined client would broadcast its initial
                // empty silo state to the host and wipe the host's silos —
                // observed in testing, hence this guard. Initial state across
                // the wire comes from ConnectHandler's snapshot push, not
                // from the reconciler.
                if (!hasPrev)
                {
                    _lastSent[key] = (typeId, count);
                    continue;
                }

                if (prev.Item1 == typeId && prev.Item2 == count)
                    continue;

                if (Main.DiagnosticLogging)
                    SrLogger.LogMessage($"[SR2MP-Diag-Silo] Reconcile diff plot={loc._id} slot={i} typeId={typeId} count={count} prev=({prev.Item1},{prev.Item2})");

                SiloBroadcaster.BroadcastSlot(silo, i);
            }
        }
    }
}
