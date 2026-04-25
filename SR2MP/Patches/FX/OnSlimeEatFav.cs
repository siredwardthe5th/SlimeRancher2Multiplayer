using HarmonyLib;
using SR2MP.Packets.FX;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(SlimeEat), nameof(SlimeEat.InvokeOnEat))]
public static class OnSlimeEatFav
{
    public static void Postfix(SlimeEat __instance, bool isFavorite)
    {
        if (handlingPacket) return;
        if (!isFavorite) return;

        Main.SendToAllOrServer(new WorldFXPacket
        {
            FX = WorldFXType.FavoriteFoodEaten,
            Position = __instance.transform.position
        });
    }
}
