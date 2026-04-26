using MelonLoader;
using SR2MP.Packets.Player;
using SR2MP.Shared.Managers;

namespace SR2MP.Components.Player;

// Periodically snapshot the local player's ammo slot inventory and send it
// to the host so the host can persist it. The server's PlayerInventoryStore
// keeps the latest snapshot keyed on player ID; on disconnect the host
// retains it; on reconnect the host pushes it back via a single
// PlayerInventoryPacket addressed to the joining player.
//
// Only client-side instances need to broadcast — the host's own inventory
// is part of its game save and isn't synced anywhere. Gating on
// Client.IsConnected covers that.
//
// 5s cadence matches Timers.PlayerInventoryTimer (which was previously
// declared but unused). Bandwidth per tick is trivial (~16 bytes per slot
// × ~10 slots = a couple hundred bytes), so a periodic full snapshot is
// cheaper to implement than dirty-tracking for no meaningful win.
[RegisterTypeInIl2Cpp(false)]
public sealed class PlayerInventoryBroadcaster : MonoBehaviour
{
    private const float SnapInterval = 5f;
    private float _nextSnap;

    private void Update()
    {
        if (!MultiplayerActive) return;
        // Host doesn't broadcast — its inventory is its own save's responsibility.
        if (Main.Client == null || !Main.Client.IsConnected) return;
        if (UnityEngine.Time.time < _nextSnap) return;
        _nextSnap = UnityEngine.Time.time + SnapInterval;

        var ps = SceneContext.Instance?.PlayerState;
        if (ps == null) return;
        var ammo = ps.Ammo;
        if (ammo == null) return;
        var slots = ammo.Slots;
        if (slots == null) return;

        var packet = new PlayerInventoryPacket
        {
            PlayerId = LocalID,
            Slots = new List<PlayerInventoryPacket.SlotData>(slots.Length),
        };

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s == null) continue;

            var typeId = s.Id != null ? NetworkActorManager.GetPersistentID(s.Id) : -1;
            packet.Slots.Add(new PlayerInventoryPacket.SlotData
            {
                SlotIndex = i,
                ActorTypeId = typeId,
                Count = s.Count,
                MaxCount = ammo.GetSlotMaxCount(i),
            });
        }

        if (Main.DiagnosticLogging)
            SrLogger.LogMessage($"[SR2MP-Diag-Inv] Broadcasting {packet.Slots.Count} slots for player={packet.PlayerId}");

        Main.SendToAllOrServer(packet);
    }
}
