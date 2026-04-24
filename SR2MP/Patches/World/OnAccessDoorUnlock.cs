using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.AccessDoor;
using Il2CppMonomiPark.SlimeRancher.UI.Framework.Displays;
using Il2CppMonomiPark.World;
using SR2MP.Packets.World;

namespace SR2MP.Patches.World;

[HarmonyPatch(typeof(AccessDoorUIActivator), nameof(AccessDoorUIActivator.OnPurchaseMenuResult))]
public static class OnAccessDoorUnlock
{
    public static void Postfix(AccessDoorUIActivator __instance, UIRuntimeDisplay display, bool result)
    {
        if (!result) return;
        var packet = new AccessDoorPacket
        {
            ID = __instance._accessDoor.Id,
            State = AccessDoor.State.OPEN
        };
        Main.SendToAllOrServer(packet);
    }
}