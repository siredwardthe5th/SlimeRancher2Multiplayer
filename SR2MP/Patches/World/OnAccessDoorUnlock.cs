using HarmonyLib;
using Il2CppMonomiPark.World;
using SR2MP.Packets.World;

namespace SR2MP.Patches.World;

// Originally patched AccessDoorUIRoot.UnlockDoor, which was removed in SR2 1.2.0.
// AccessDoor.set_CurrState is the underlying state mutator and exists unchanged
// in both 1.1.1 and 1.2.0. Patching the setter catches state changes from any
// source (UI unlock, scripted unlock, etc.).
//
// The receive handler (Client/Handlers/AccessDoorHandler.cs) already sets state
// via the same property, gated by handlingPacket so this Postfix won't echo it
// back over the network.
[HarmonyPatch(typeof(AccessDoor), "set_CurrState")]
public static class OnAccessDoorStateChanged
{
    public static void Postfix(AccessDoor __instance, AccessDoor.State value)
    {
        if (handlingPacket) return;
        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;
        if (value != AccessDoor.State.OPEN) return;

        var doors = SceneContext.Instance?.GameModel?.doors;
        if (doors == null) return;

        // Reverse-lookup the door's ID from the GameObject. Doors are rare and
        // state changes are rarer, so the O(N) walk is acceptable.
        string doorId = null!;
        foreach (var entry in doors)
        {
            if (entry.value?.gameObj == __instance.gameObject)
            {
                doorId = entry.key;
                break;
            }
        }

        if (string.IsNullOrEmpty(doorId)) return;

        var packet = new AccessDoorPacket
        {
            ID = doorId,
            State = AccessDoor.State.OPEN,
        };
        Main.SendToAllOrServer(packet);
    }
}
