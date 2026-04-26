// Disabled in 1.2.0:
// InstantiateActor's signature gained a Nullable<AmmoSlot.AmmoMetadata> parameter
// in SR2 1.2.0. MelonLoader 0.7.2's Il2CppInterop fails to marshal that parameter
// when invoking the patched managed wrapper, so EVERY call to InstantiateActor
// (every actor spawn) throws NullReferenceException in the il2cpp->managed
// trampoline. The exception is swallowed by Il2CppInterop, but spawned actors
// end up partially initialized — slimes whose AI components depend on full
// initialization can no longer eat (SlimeEat.MaybeChomp returns False).
//
// Multiplayer regression: locally-spawned actors will not be broadcast to
// remote players via this hook. Server- and client-received spawns still flow
// through NetworkActorManager.TrySpawnNetworkActor, which is unaffected.
//
// To restore: re-add the [HarmonyPatch] attribute below once Il2CppInterop
// supports marshaling Nullable<Il2CppValueType> for the new InstantiateActor
// overload, OR retarget this patch at a different sentinel method that fires
// on every actor spawn (e.g. IdentifiableActor.Awake).
/*
using System.Collections;
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using MelonLoader;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(InstantiationHelpers), nameof(InstantiationHelpers.InstantiateActor))]
public static class OnActorSpawn
{
    private static IEnumerator SpawnOverNetwork(int actorType, byte sceneGroup, GameObject actor)
    {
        yield return null;

        if (!actor)
            yield break;

        var id = actor.GetComponent<IdentifiableActor>().GetActorId();

        var packet = new ActorSpawnPacket
        {
            ActorType = actorType,
            SceneGroup = sceneGroup,
            ActorId = id,
            Position = actor.transform.position,
            Rotation = actor.transform.rotation,
        };

        Main.SendToAllOrServer(packet);

        actorManager.Actors[id.Value] = actor.GetComponent<IdentifiableActor>()._model;
    }

    public static void Postfix(
        GameObject __result,
        GameObject original,
        SceneGroup sceneGroup)
    {
        if (handlingPacket) return;
        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;

        __result.AddComponent<NetworkActor>().LocallyOwned = true;

        var actorType = NetworkActorManager.GetPersistentID(original.GetComponent<Identifiable>().identType);
        var sceneGroupId = NetworkSceneManager.GetPersistentID(sceneGroup);

        MelonCoroutines.Start(SpawnOverNetwork(actorType, (byte)sceneGroupId, __result));
    }
}
*/
