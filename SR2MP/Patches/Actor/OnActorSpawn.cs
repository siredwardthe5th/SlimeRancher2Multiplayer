using System.Collections;
using HarmonyLib;
using MelonLoader;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Actor;

// Originally patched InstantiationHelpers.InstantiateActor. In SR2 1.2.0 that
// method gained a Nullable<MonomiPark.SlimeRancher.Player.AmmoSlot.AmmoMetadata>
// parameter, which MelonLoader 0.7.2's Il2CppInterop cannot marshal — every
// actor spawn threw NullReferenceException in the il2cpp->managed trampoline,
// leaving newly-spawned actors partially initialized (e.g. SlimeEat.MaybeChomp
// returned False on every chomp attempt for new slimes).
//
// Retargeted to IdentifiableActor.Awake, which fires once per actor as the
// MonoBehaviour wakes up. Awake itself takes no parameters, so there is nothing
// to marshal. We defer the actual broadcast to the next frame because the
// actor's model is not populated until InitModel runs after Awake.
//
// Filtering rules:
//   - skip during scene load (covers initial save load)
//   - skip if NetworkActor is already attached (covers actors added via
//     OnGameLoadPatch on server start, and actors received from network via
//     NetworkActorManager.TrySpawnNetworkActor / ZoneLoadingLoop)
//   - skip if the actor's id is already in actorManager.Actors (defense in
//     depth against double-broadcasting)
//   - the existing handlingPacket guard suppresses re-broadcast when the
//     actor was instantiated as a result of an incoming packet
[HarmonyPatch(typeof(IdentifiableActor), nameof(IdentifiableActor.Awake))]
public static class OnActorSpawn
{
    public static void Postfix(IdentifiableActor __instance)
    {
        if (handlingPacket) return;
        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;

        var sceneLoader = SystemContext.Instance?.SceneLoader;
        if (sceneLoader == null || sceneLoader.IsSceneLoadInProgress) return;

        if (__instance.GetComponent<NetworkActor>()) return;

        MelonCoroutines.Start(BroadcastActor(__instance));
    }

    private static IEnumerator BroadcastActor(IdentifiableActor actor)
    {
        // Wait for InitModel to populate _model with id / sceneGroup / etc.
        yield return null;

        if (!actor) yield break;
        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) yield break;
        if (actor.GetComponent<NetworkActor>()) yield break;

        var actorId = actor.GetActorId();
        if (actorId.Value == 0) yield break;
        if (actorManager.Actors.ContainsKey(actorId.Value)) yield break;

        var ident = actor.GetComponent<Identifiable>();
        if (!ident) yield break;

        var model = actor._model;
        if (model == null) yield break;

        actor.gameObject.AddComponent<NetworkActor>().LocallyOwned = true;

        var actorType = NetworkActorManager.GetPersistentID(ident.identType);
        var sceneGroupId = NetworkSceneManager.GetPersistentID(model.sceneGroup);

        var packet = new ActorSpawnPacket
        {
            ActorType = actorType,
            SceneGroup = (byte)sceneGroupId,
            ActorId = actorId,
            Position = actor.transform.position,
            Rotation = actor.transform.rotation,
        };

        Main.SendToAllOrServer(packet);
        actorManager.Actors[actorId.Value] = model;
    }
}
