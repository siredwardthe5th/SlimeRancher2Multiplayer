using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using SR2MP.Packets.Player;

namespace SR2MP.Server.Managers;

// Server-side persistence for remote players' inventories. The user's spec
// is that the host owns each remote player's inventory state — when a player
// disconnects, the host retains their last-broadcast inventory; when they
// reconnect (within the session OR across host restarts), the host pushes
// that snapshot back.
//
// Storage: in-memory dictionary keyed on player ID, mirrored to a JSON file
// at UserData/SR2MP/player_inventories.json. The file is loaded lazily on
// first access and written atomically (.tmp + move) on every save so a
// crash mid-write can't truncate the file.
//
// Static rather than DI'd — there's only one server instance and the data
// has to outlive the host process.
internal static class PlayerInventoryStore
{
    private static readonly Dictionary<string, PlayerInventoryPacket> _store = new();
    private static bool _loaded;
    private static readonly object _ioLock = new();

    private static string SaveDir => Path.Combine(MelonEnvironment.UserDataDirectory, "SR2MP");
    private static string SavePath => Path.Combine(SaveDir, "player_inventories.json");
    private static string TmpPath => SavePath + ".tmp";

    public static void Save(string playerId, PlayerInventoryPacket packet)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        EnsureLoaded();

        // Take a copy of the slot list so subsequent mutation of the
        // received packet (it's a class, not a struct) can't poison the
        // saved snapshot.
        var slotsCopy = packet.Slots != null
            ? new List<PlayerInventoryPacket.SlotData>(packet.Slots)
            : new List<PlayerInventoryPacket.SlotData>();

        _store[playerId] = new PlayerInventoryPacket
        {
            PlayerId = playerId,
            Slots = slotsCopy,
        };

        WriteToDisk();
    }

    public static bool TryLoad(string playerId, out PlayerInventoryPacket packet)
    {
        EnsureLoaded();
        return _store.TryGetValue(playerId, out packet!);
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true; // set first so a load failure doesn't retry on every call

        try
        {
            if (!File.Exists(SavePath))
            {
                MelonLogger.Msg("[SR2MP/InventoryStore] No saved inventories file, starting fresh");
                return;
            }

            var json = File.ReadAllText(SavePath);
            var entries = JsonConvert.DeserializeObject<List<PersistedInventory>>(json);
            if (entries == null) return;

            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.PlayerId)) continue;
                _store[e.PlayerId] = new PlayerInventoryPacket
                {
                    PlayerId = e.PlayerId,
                    Slots = e.Slots ?? new List<PlayerInventoryPacket.SlotData>(),
                };
            }

            MelonLogger.Msg($"[SR2MP/InventoryStore] Loaded inventories for {_store.Count} player(s) from {SavePath}");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[SR2MP/InventoryStore] Failed to load {SavePath}: {ex}");
        }
    }

    private static void WriteToDisk()
    {
        // Serialize on the calling thread but inside a lock so concurrent
        // Save() calls (in theory possible from different packet threads)
        // don't interleave file writes.
        lock (_ioLock)
        {
            try
            {
                Directory.CreateDirectory(SaveDir);

                var entries = new List<PersistedInventory>(_store.Count);
                foreach (var kv in _store)
                    entries.Add(new PersistedInventory { PlayerId = kv.Key, Slots = kv.Value.Slots });

                var json = JsonConvert.SerializeObject(entries, Formatting.Indented);

                // Atomic-ish write: dump to .tmp first, then move into place.
                // Avoids leaving a half-written file if the process dies.
                File.WriteAllText(TmpPath, json);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(TmpPath, SavePath);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SR2MP/InventoryStore] Failed to write {SavePath}: {ex}");
            }
        }
    }

    // Plain DTO for JSON. Mirrors PlayerInventoryPacket but isolated from
    // any IPacket / network concerns so the on-disk format doesn't drift if
    // the packet schema changes.
    private sealed class PersistedInventory
    {
        public string PlayerId { get; set; } = string.Empty;
        public List<PlayerInventoryPacket.SlotData> Slots { get; set; } = new();
    }
}
