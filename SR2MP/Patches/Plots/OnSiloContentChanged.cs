using HarmonyLib;
using SR2MP.Components.World;
using SR2MP.Packets.Landplot;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Plots;

// Captures silo state changes and broadcasts the new slot snapshot. We hook
// the methods that mutate slot count and emit the *resulting* state from
// GetSlot* — receivers apply the absolute count and don't need to know what
// the change was.
//
// Two coverage paths:
//   - MaybeAddAsResource: caller specifies slotIdx, we broadcast that one slot.
//   - MaybeAddToAnySlot / OnIdentifiableRemoved: caller doesn't specify a slot,
//     so we broadcast ALL slots in the silo. Silos have <= ~5 slots so this
//     is cheap.
//
// The handlingPacket guard suppresses re-broadcast when the change is itself
// the result of an incoming SiloContentPacket.

[HarmonyPatch(typeof(SiloStorage), nameof(SiloStorage.MaybeAddAsResource))]
public static class OnSiloAddResource
{
    public static void Postfix(SiloStorage __instance, int slotIdx, bool __result)
    {
        if (handlingPacket || !__result) return;
        if (!MultiplayerActive) return;
        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Silo] MaybeAddAsResource Postfix slot={slotIdx} result={__result}");
        SiloBroadcaster.BroadcastSlot(__instance, slotIdx);
    }
}

[HarmonyPatch(typeof(SiloStorage), nameof(SiloStorage.MaybeAddToAnySlot))]
public static class OnSiloAddAnySlot
{
    public static void Postfix(SiloStorage __instance, bool __result)
    {
        if (handlingPacket || !__result) return;
        if (!MultiplayerActive) return;
        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Silo] MaybeAddToAnySlot Postfix result={__result}");
        SiloBroadcaster.BroadcastAllSlots(__instance);
    }
}

// Vacuuming an item INTO a silo / feeder (vs the catcher trigger) probably
// routes through MaybeAddToSpecificSlot. Without this the auto-feeder fill
// never broadcasts to remote players.
[HarmonyPatch(typeof(SiloStorage), nameof(SiloStorage.MaybeAddToSpecificSlot))]
public static class OnSiloAddSpecificSlot
{
    public static void Postfix(SiloStorage __instance, int slotIdx, bool __result)
    {
        if (handlingPacket || !__result) return;
        if (!MultiplayerActive) return;
        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Silo] MaybeAddToSpecificSlot Postfix slot={slotIdx} result={__result}");
        SiloBroadcaster.BroadcastSlot(__instance, slotIdx);
    }
}

// Disabled: SiloStorage.OnIdentifiableRemoved fires during MelonLoader's
// early IL2CPP type registration phase, before Main is initialized. With
// MultiplayerActive null-safe the Postfix returns early — but the patched
// trampoline being invoked at all during this window appears to corrupt
// IL2CPP state and hard-crash the process (process termination with no
// managed exception, log stops at "Registered mono icall ... SetAsLastSibling").
//
// Other SiloStorage patches (MaybeAddAsResource, MaybeAddToAnySlot) only
// fire from gameplay code paths, not during IL2CPP setup, so they're safe.
//
// Trade-off: silo removals don't broadcast directly. The only path to remove
// items from a silo in normal play is the player vacuuming them out, which
// goes through Vacuumable.Capture → already patched. Items "consumed" by
// Slime Feeder would also fire this, but feeder consumption is host-authoritative
// (gated by ServerAuthorityGates when that's re-enabled), so clients shouldn't
// see drift from missed broadcasts there either.
/*
[HarmonyPatch(typeof(SiloStorage), nameof(SiloStorage.OnIdentifiableRemoved))]
public static class OnSiloRemove
{
    public static void Postfix(SiloStorage __instance)
    {
        if (handlingPacket) return;
        if (!MultiplayerActive) return;
        SiloBroadcaster.BroadcastAllSlots(__instance);
    }
}
*/

internal static class SiloBroadcaster
{
    // ADD-side broadcasts had an off-by-one: GetSlotCount in the Postfix of
    // SiloStorage.MaybeAdd* returned the PRE-add count (the underlying slot
    // hasn't been mutated yet, possibly because MaybeAdd schedules the add
    // via a callback rather than applying it inline). Defer one frame so
    // the count we read is the actual post-add value.
    public static void BroadcastSlot(SiloStorage silo, int slotIdx)
    {
        // Broadcast immediately. Earlier we deferred by one frame to "let
        // the slot mutation land", but that collapsed multiple rapid
        // adds/removes into a single broadcast: e.g. user adds 5→6 then
        // 6→7 in successive frames, both deferred broadcasts read count=7
        // (the final state) so intermediate count=6 was never sent. Host
        // saw the count jump 5→7. Reading immediately preserves every
        // intermediate state.
        if (!silo) return;
        var loc = silo.GetComponentInParent<LandPlotLocation>();
        if (loc == null) return;
        SendOne(loc._id, silo, slotIdx);
    }

    public static void BroadcastAllSlots(SiloStorage silo)
    {
        if (!silo) return;
        var loc = silo.GetComponentInParent<LandPlotLocation>();
        if (loc == null) return;

        var ammo = silo.GetRelevantAmmo();
        if (ammo == null) return;

        var slots = ammo.Slots;
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
            SendOne(loc._id, silo, i);
    }

    // Per-sender monotonic counter so the receive-side log can be correlated
    // to the send-side log without relying on wall-clock alignment between
    // machines. The remote (host) clock can drift seconds behind/ahead of
    // ours; with sequence stamping we can grep `seq=42` in both logs and
    // see exactly which send maps to which apply.
    private static uint _sendSequence;

    private static void SendOne(string plotId, SiloStorage silo, int slotIdx)
    {
        var ident = silo.GetSlotIdentifiable(slotIdx);
        var count = silo.GetSlotCount(slotIdx);

        var typeId = ident != null
            ? NetworkActorManager.GetPersistentID(ident)
            : -1;

        var seq = ++_sendSequence;

        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Silo] Sending seq={seq} plot={plotId} slot={slotIdx} typeId={typeId} count={count}");

        // Record in the reconciler cache BEFORE sending so that if the
        // reconciler ticks immediately after, it sees no diff and doesn't
        // re-broadcast the same state.
        SiloReconciler.RecordState(plotId, slotIdx, typeId, count);

        Main.SendToAllOrServer(new SiloContentPacket
        {
            PlotID = plotId,
            SlotIndex = slotIdx,
            ActorTypeId = typeId,
            Count = count,
            Sequence = seq,
        });
    }
}
