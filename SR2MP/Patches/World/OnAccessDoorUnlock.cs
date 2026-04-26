// Disabled: AccessDoorUIRoot.UnlockDoor was renamed/removed in SR2 1.1.x.
// Multiplayer access-door unlock sync is non-functional in this build.
// Re-enable via the official build pipeline targeting the matching SR2 version.
/*
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.AccessDoor;
using Il2CppMonomiPark.World;
using SR2MP.Packets.World;

namespace SR2MP.Patches.World;

[HarmonyPatch(typeof(AccessDoorUIRoot), nameof(AccessDoorUIRoot.UnlockDoor))]
public static class OnAccessDoorUnlock
{
    public static void Postfix(AccessDoorUIRoot __instance)
    {
        var packet = new AccessDoorPacket
        {
            ID = __instance._door.Id,
            State = AccessDoor.State.OPEN
        };
        Main.SendToAllOrServer(packet);
    }
}
*/
