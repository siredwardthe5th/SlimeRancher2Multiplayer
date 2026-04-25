using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.VFX.EnvironmentInteraction;
using SR2MP.Packets.FX;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(EnvironmentInteractionEntity), nameof(EnvironmentInteractionEntity.WaterTouchStart))]
public static class OnWaterTouchStart
{
    public static void Postfix(EnvironmentInteractionEntity __instance)
    {
        if (handlingPacket) return;
        if (!__instance.GetComponent<PlayerState>()) return;

        var packet = new PlayerFXPacket
        {
            FX = PlayerFXType.WaterSplash,
            Position = __instance.transform.position
        };

        Main.SendToAllOrServer(packet);
    }
}
