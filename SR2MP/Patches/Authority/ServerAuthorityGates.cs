using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World;

namespace SR2MP.Patches.Authority;

// Server-authority gates. These patches make the host the sole driver of
// gameplay state changes that produce persistent results (loot, currency,
// auto-feed, auto-collect). Without them, both halves of multiplayer would
// run the same simulation loops independently and produce duplicate state.
//
// Returning `false` from a Prefix on Il2Cpp methods skips the original method.
// On singleplayer (no server, no client) the predicate evaluates to true (we
// fall through and the original runs as normal).
//
// IMPORTANT: these gates change game behavior on connected clients. Things
// the original methods did purely for visual/audio purposes will stop happening
// on clients — the assumption is that the corresponding network packets will
// recreate those effects. If something that should be visible on a client
// stops appearing during testing, it likely needs a packet/handler pair.

internal static class Auth
{
    public static bool IsClient => Main.Client != null && Main.Client.IsConnected;
}

// Plort Collector: first version of this gate skipped DoCollection on clients
// to avoid double-collection. In testing that broke a real case — a plort that
// exists only in the client's loaded view (e.g. host's plot is hibernated for
// the host) never gets collected because the host's loop sees nothing and
// the client's loop is gated.
//
// Currency packets carry an absolute NewAmount, so even if both halves credit
// independently the next packet brings them back into sync. Disabling this
// gate is the lesser evil pending a proper authority refactor (suppress only
// when the host has the plot region active).
/*
[HarmonyPatch(typeof(PlortCollector), nameof(PlortCollector.DoCollection))]
internal static class GatePlortCollectorDoCollection
{
    public static bool Prefix()
    {
        if (Auth.IsClient)
        {
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage("[SR2MP-Diag-Auth] Skipped PlortCollector.DoCollection (client)");
            return false;
        }
        return true;
    }
}
*/

// Slime Feeder: SlimeFeeder.EjectFood drops a food actor into the pen. If both
// halves run it, food doubles. Skip on clients; the host's food spawn is
// broadcast via the actor-spawn pipeline.
[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.EjectFood))]
internal static class GateSlimeFeederEject
{
    public static bool Prefix()
    {
        if (Auth.IsClient)
        {
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage("[SR2MP-Diag-Auth] Skipped SlimeFeeder.EjectFood (client)");
            return false;
        }
        return true;
    }
}

// Lightning: when a network-spawned LightningStrike runs, it deals damage and
// drops loot just like a host-spawned one. The host already receives the
// resulting actor spawns via the actor-spawn pipeline, so client-side network
// lightning should be visual-only. We detect "network-spawned" by the "(Net)"
// suffix the LightningStrikeHandler appends to the GameObject name.
//
// Note: this runs on EVERY LightningStrike, host and client. Local-spawned
// lightning (no "(Net)" suffix) still runs normally on the host.
[HarmonyPatch(typeof(LightningStrike), nameof(LightningStrike.Start))]
internal static class GateNetworkLightning
{
    public static bool Prefix(LightningStrike __instance)
    {
        if (!Main.Client.IsConnected) return true;

        // Only short-circuit the network-mirrored copy — we still want the
        // host's own lightning to run its full chain. The (Net) suffix is
        // added by Client/Handlers/LightningStrikeHandler.cs immediately
        // after Object.Instantiate, before the prefab's Start() fires.
        if (__instance.gameObject.name.Contains("net", StringComparison.InvariantCultureIgnoreCase))
        {
            if (Main.DiagnosticLogging)
                SrLogger.LogMessage($"[SR2MP-Diag-Auth] Skipped LightningStrike.Start on network-mirrored '{__instance.name}'");
            return false;
        }
        return true;
    }
}
