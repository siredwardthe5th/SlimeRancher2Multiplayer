using HarmonyLib;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Weather;

[HarmonyPatch(typeof(StaticTornadoSpawner), nameof(StaticTornadoSpawner.Spawn))]
public static class OnTornadoSpawn
{
    public static bool Prefix()
    {
        if (handlingPacket) return true;
        return !Main.Client.IsConnected || Main.Server.IsRunning();
    }

    public static void Postfix(Vector3 position, Quaternion rotation)
    {
        if (handlingPacket) return;
        Main.SendToAllOrServer(new TornadoSpawnPacket { Position = position, Rotation = rotation });
    }
}
