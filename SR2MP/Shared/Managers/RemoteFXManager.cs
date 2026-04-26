using SR2E.Utils;
using SR2MP.Components.FX;

namespace SR2MP.Shared.Managers;

public sealed class RemoteFXManager
{
    // ReSharper disable once MemberCanBePrivate.Global
    public readonly Dictionary<string, GameObject> AllFX = new();
    public readonly Dictionary<string, SECTR_AudioCue> AllCues = new();

    public Dictionary<PlayerFXType, GameObject> PlayerFXMap;
    public Dictionary<PlayerFXType, SECTR_AudioCue> PlayerAudioCueMap;

    public Dictionary<WorldFXType, GameObject> WorldFXMap;
    public Dictionary<WorldFXType, SECTR_AudioCue> WorldAudioCueMap;

    public GameObject FootstepFX;
    public GameObject? SellFX;

    // Case-insensitive substring lookup for FX whose exact prefab name we don't
    // know at compile time. Returns the first match in AllFX or null.
    private GameObject FindFX(string substring)
    {
        foreach (var kv in AllFX)
        {
            if (kv.Key.Contains(substring, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }
        return null!;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BONE-ATTACH HELPERS (preserved for future re-attempt; see
    // Patches/FX/OnVacuumFXLifecycle.cs for the full investigation writeup).
    //
    // Goal was to attach the vac trail FX to a specific bone on the remote
    // player so it tracks the gun pose. Every candidate had a problem:
    //   - mesh_vacpac is hip-parented (no arm tracking) and tilted
    //   - joint11 (hose tip) lives under inactive parent → FX inherits inactive
    //   - rWristJ would track arm but isn't where the vac actually is
    // Reverted to chest-attached for now. To re-enable bone attachment, restore
    // these helpers and the corresponding spawn code in the receive handlers.
    /*
    public static Transform? FindRightHandTransform(Transform root)
    {
        // Priority order matters: joint11 is a *grandchild* of mesh_vacpac,
        // so a single combined predicate would always pick mesh_vacpac first
        // (depth-first walk visits the parent first). Run separate searches
        // and take the first non-null match.
        //
        // Also: any candidate must be in an active hierarchy. The hose
        // joints (joint11) live under mesh_vacpac_hose which is sometimes
        // disabled on the BeatrixMainMenu rig — attaching to an inactive
        // parent makes the FX child inherit `activeInHierarchy=false` and
        // it never renders.
        Transform? FindNamedActive(string name) =>
            FindInHierarchy(root, t => t.name == name && t.gameObject.activeInHierarchy);

        return FindNamedActive("joint11")          // hose muzzle — forward axis = aim direction
            ?? FindNamedActive("anchor_joint21")   // hose anchor — second-best on the hose
            ?? FindNamedActive("mesh_vacpac")      // gun body — works but tilted
            ?? FindNamedActive("rWristJ")          // right wrist (non-vac humanoid models)
            ?? FindInHierarchy(root, t =>
            {
                if (!t.gameObject.activeInHierarchy) return false;
                var n = t.name.ToLowerInvariant().Replace(" ", "");
                foreach (var p in new[] { "righthand", "hand_r", "hand.r", "r_hand", "rhand", "wrist_r", "wrist.r" })
                    if (n.Contains(p)) return true;
                return false;
            });
    }

    private static Transform? FindInHierarchy(Transform root, Func<Transform, bool> match)
    {
        if (match(root)) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var found = FindInHierarchy(child, match);
            if (found != null) return found;
        }
        return null;
    }

    // One-shot debug: dump the entire transform hierarchy under `root`. Useful
    // when we need to know what bones exist on the remote player model so we
    // can pick a correct attach point. Was invoked once per player from the
    // receive handlers and emitted [SR2MP-Diag-Hier] log entries.
    public static void DumpHierarchy(Transform root, string tag)
    {
        DumpHierarchyRecursive(root, tag, 0);
    }

    private static void DumpHierarchyRecursive(Transform t, string tag, int depth)
    {
        SrLogger.LogMessage($"[SR2MP-Diag-Hier] {tag} {new string(' ', depth * 2)}{t.name}");
        for (int i = 0; i < t.childCount; i++)
            DumpHierarchyRecursive(t.GetChild(i), tag, depth + 1);
    }
    */

    // Re-resolve a PlayerFX prefab from currently-loaded ParticleSystemRenderer
    // objects. Used as a fallback when a cached PlayerFXMap entry's GameObject
    // got destroyed between Initialize() and use (Unity-side cleanup of
    // transient FX instances). Falls back through the same priority list as
    // the original Initialize lookup.
    public GameObject? RefreshVacTrailPrefab()
    {
        var resources = Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>();

        GameObject? FirstMatch(string sub)
        {
            foreach (var p in resources)
            {
                if (!p) continue;
                var go = p.gameObject;
                if (!go) continue;
                if (go.name.Contains(sub, StringComparison.OrdinalIgnoreCase))
                    return go;
            }
            return null;
        }

        var fresh = FirstMatch("FX_Vac_Dust") ?? FirstMatch("FX Vac Dust")
                 ?? FirstMatch("FX_Vac_Rings") ?? FirstMatch("FX Vac Rings")
                 ?? FirstMatch("Droplets_Vacced")
                 ?? FirstMatch("VacuumTrail") ?? FirstMatch("VacTrail")
                 ?? FirstMatch("PS_VacuumDirectional") ?? FirstMatch("PS VacuumDirectional");

        if (fresh != null)
            PlayerFXMap[PlayerFXType.VacTrail] = fresh;

        return fresh;
    }

    private SECTR_AudioCue FindCue(string substring)
    {
        foreach (var kv in AllCues)
        {
            if (kv.Key.Contains(substring, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }
        return null!;
    }

    private static Predicate<SECTR_AudioCue> Force3DCondition => cue =>
    {
        // Movement SFX
        if (cue.name.Contains("Step")
            || cue.name.Contains("Run")
            || cue.name.Contains("Jump")
            || cue.name.Contains("Land"))
        {
            return true;
        }

        // VAC SFX
        if (cue.name.Contains("VacAmmoSelect"))
            return false;

        if (cue.name.Contains("vac", StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    };

    internal void Initialize()
    {
        AllFX.Clear();
        var resources = Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>();
        foreach (var particle in resources)
        {
            var particleName = particle.gameObject.name.Replace(' ', '_');

            AllFX.TryAdd(particleName, particle.gameObject);
        }
        AllCues.Clear();
        foreach (var cue in Resources.FindObjectsOfTypeAll<SECTR_AudioCue>())
        {
            if (cue.Spatialization != SECTR_AudioCue.Spatializations.Simple2D)
                cue.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;

            if (Force3DCondition(cue))
                cue.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;

            var cueName = cue.name.Replace(' ', '_');
            AllCues.TryAdd(cueName, cue);
        }
        PlayerFXMap = new Dictionary<PlayerFXType, GameObject>
        {
            { PlayerFXType.None, null! },
            { PlayerFXType.VacReject, AllFX["FX_vacReject"] },
            { PlayerFXType.VacAccept, AllFX["FX_vacAcquire"] },
            { PlayerFXType.VacShoot, AllFX["FX_VacpackShoot"] },
            // Substring lookups for FX whose exact prefab name we don't know;
            // null is acceptable and the receive handler tolerates it.
            { PlayerFXType.WaterSplash, FindFX("WaterSplash") ?? FindFX("Splash") },
            { PlayerFXType.WalkTrail, FindFX("WalkTrail") ?? FindFX("FootTrail") },
            // VacTrail is the visible "vacuum stream" effect attached to a remote
            // player while they hold the vac button. Original lookup found
            // nothing on 1.2.0 — diag dump shows the actual prefab names are
            // FX_Vac_Rings / FX_Vac_Dust / PS_VacuumDirectional.
            //
            // Tried PS_VacuumDirectional first: spawns + reports active=True
            // but emits nothing — it's a directional stream that needs the
            // vacuum aim/distance shader inputs (`vacWindDistance`, etc.) to
            // visibly emit. Detached from the vac aim logic it's inert.
            //
            // FX_Vac_Dust / FX_Vac_Rings are simpler ambient particle systems
            // that emit on Play() without external inputs. Try those first
            // and only fall back to the directional one as last resort.
            { PlayerFXType.VacTrail, FindFX("FX_Vac_Dust") ?? FindFX("FX_Vac_Rings") ?? FindFX("Droplets_Vacced") ?? FindFX("VacuumTrail") ?? FindFX("VacTrail") ?? FindFX("PS_VacuumDirectional") ?? FindFX("VacFX") },
        };
        PlayerAudioCueMap = new Dictionary<PlayerFXType, SECTR_AudioCue>
        {
            { PlayerFXType.None, null! },
            { PlayerFXType.VacShootEmpty, AllCues["VacShootEmpty"]},
            { PlayerFXType.VacHold, AllCues["VacClogged"]},
            { PlayerFXType.VacSlotChange, AllCues["VacAmmoSelect"]},
            { PlayerFXType.VacRunning, AllCues["VacRun"]},
            { PlayerFXType.VacRunningStart, AllCues["VacStart"]},
            { PlayerFXType.VacRunningEnd, AllCues["VacEnd"]},
            { PlayerFXType.VacShootSound, AllCues["VacShoot"]},
            { PlayerFXType.WaterSplashSound, FindCue("WaterSplash") ?? FindCue("Splash") ?? FindCue("Water") },
        };
        WorldFXMap = new Dictionary<WorldFXType, GameObject>
        {
            { WorldFXType.None, null! },
            { WorldFXType.SellPlort, SellFX ?? AllFX["FX_Stars"] },
            { WorldFXType.FavoriteFoodEaten, AllFX["FX_slimeEatFav"] },
            { WorldFXType.GordoFoodEaten, AllFX["FX_Gordo_Eat"] },
        };
        WorldAudioCueMap = new Dictionary<WorldFXType, SECTR_AudioCue>
        {
            { WorldFXType.None, null! },
            { WorldFXType.BuyPlot, AllCues["PurchaseRanchTechBase"]},
            { WorldFXType.UpgradePlot, AllCues["PurchaseRanchTechUpgrade"]},
            { WorldFXType.SellPlortSound, AllCues["SiloReward"]},
            { WorldFXType.SellPlortDroneSound, AllCues["SiloRewardDrone"]},
            { WorldFXType.GordoFoodEatenSound, AllCues["GordoGulp"] },
           // { WorldFXType.FabricatorPurchaseGadget, AllCues["PurchaseGadget"] },
           // { WorldFXType.FabricatorPurchaseGadget, AllCues["Click3"] },
           // { WorldFXType.FabricatorPurchaseUpgrade, AllCues["PurchaseFabricatorUpgrade"] },
        };

        foreach (var (playerFX, obj) in PlayerFXMap)
        {
            if (!obj)
                continue;

            // Please Az find a better way :sob:
            // Made slight improvements - Az
            foreach (var particle in resources.Where(x => x.name.Contains(obj.name)))
            {
                if (!particle.GetComponent<NetworkPlayerFX>())
                    particle.AddComponent<NetworkPlayerFX>().fxType = playerFX;
            }
        }

        foreach (var (worldFX, obj) in WorldFXMap)
        {
            if (!obj)
                continue;

            foreach (var particle in resources.Where(x => x.name.Contains(obj.name)))
            {
                if (!particle.GetComponent<NetworkWorldFX>())
                    particle.AddComponent<NetworkWorldFX>().fxType = worldFX;
            }
        }

        FootstepFX = AllFX["FX_Footstep"];

        foreach (var cue in PlayerAudioCueMap)
        {
            if (cue.Value)
                cue.Value.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;
        }
        foreach (var cue in WorldAudioCueMap)
        {
            if (cue.Value)
                cue.Value.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;
        }

        SrLogger.LogMessage("RemoteFXManager initialized", SrLogTarget.Both);

        if (Main.DiagnosticLogging)
        {
            // Dump every FX prefab name and audio cue name so we can find the
            // right names for water splash, vac trail, garden FX, etc.
            // grep MelonLogger output for [SR2MP-Diag-FX] / [SR2MP-Diag-Cue].
            var fxNames = new List<string>(AllFX.Keys);
            fxNames.Sort();
            foreach (var n in fxNames) SrLogger.LogMessage($"[SR2MP-Diag-FX] {n}");

            var cueNames = new List<string>(AllCues.Keys);
            cueNames.Sort();
            foreach (var n in cueNames) SrLogger.LogMessage($"[SR2MP-Diag-Cue] {n}");

            SrLogger.LogMessage($"[SR2MP-Diag-FX] (total {fxNames.Count} FX prefabs)");
            SrLogger.LogMessage($"[SR2MP-Diag-Cue] (total {cueNames.Count} audio cues)");

            // Report which Player FX have a resolved (non-null) prefab/cue.
            foreach (var (k, v) in PlayerFXMap)
                SrLogger.LogMessage($"[SR2MP-Diag-Map] PlayerFXMap[{k}] = {(v ? v.name : "<null>")}");
            foreach (var (k, v) in PlayerAudioCueMap)
                SrLogger.LogMessage($"[SR2MP-Diag-Map] PlayerAudioCueMap[{k}] = {(v ? v.name : "<null>")}");
        }
    }

    public bool TryGetFXType(SECTR_AudioCue cue, out PlayerFXType fxType) => TryGetFXType(cue, PlayerAudioCueMap, out fxType);

    public bool TryGetFXType(SECTR_AudioCue cue, out WorldFXType fxType) => TryGetFXType(cue, WorldAudioCueMap, out fxType);

    private static bool TryGetFXType<T>(SECTR_AudioCue cue, Dictionary<T, SECTR_AudioCue>? cueMap, out T fxType) where T : struct, Enum
    {
        fxType = default;

        if (cueMap == null)
            return false;

        foreach (var pair in cueMap)
        {
            if (pair.Value != cue)
                continue;

            fxType = pair.Key;
            return true;
        }

        return false;
    }

    public static void PlayTransientAudio(SECTR_AudioCue cue, Vector3 position, bool loop = false)
    {
        SECTR_AudioSystem.Play(cue, position, loop);
    }

    public static void PlayTransientAudio(SECTR_AudioCue cue, Vector3 position, float volume, bool loop = false)
    {
        var played = SECTR_AudioSystem.Play(cue, position, loop);

        played.Volume = volume;
    }
}