using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using SR2E.Utils;

namespace SR2MP.Patches.Compatibility;

// SR2E v3.7.0 cannot load its asset bundle on SR2 1.2.0 / Unity 6 / current
// Il2CppInterop. Symptom in MelonLoader log:
//
//   [Il2CppInterop] During invoking native->managed trampoline
//   Il2CppInterop.Runtime.ObjectCollectedException: Object was garbage
//   collected in IL2CPP domain
//      at Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase.get_Pointer()
//      at Il2CppSystem.Span`1..ctor(Il2CppArrayBase`1 array)
//      at UnityEngine.AssetBundle.LoadFromMemory_Internal(Il2CppStructArray`1 binary, UInt32 crc)
//
// Followed by NRE in SR2E.Patches.Context.SystemContextPatch.Postfix line 53
// because the returned Bundle is null. The cascade kills the whole menu
// pipeline — no menu prefabs load → SR2EModMenu / SR2ECheatMenu never get
// instantiated → MenuEUtil.GetMenu<T>() returns null → the lambdas attached
// to the pause-menu Mods/Cheats buttons NRE on .Open().
//
// Root cause: the byte[] passed to LoadFromMemory is converted to a
// Il2CppStructArray internally; the wrapper becomes GC-eligible during the
// native call and Span<>'s ctor blows up reading its Pointer.
//
// Workaround: replace SR2E.EmbeddedResourceEUtil.LoadIl2CppBundle's body
// with a Prefix that reads the embedded resource, writes it to a temp file,
// and loads via Il2CppAssetBundleManager.LoadFromFile(path) — which avoids
// the byte[] / Span marshaling entirely. The temp file is left in place
// because Unity may keep it mapped while the bundle is in use; cleaning up
// would risk later asset-load failures.
[HarmonyPatch]
internal static class Sr2eAssetBundleFix
{
    private static MethodBase? TargetMethod()
    {
        try
        {
            return typeof(EmbeddedResourceEUtil).GetMethod(
                nameof(EmbeddedResourceEUtil.LoadIl2CppBundle),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(Assembly) },
                null);
        }
        catch
        {
            return null;
        }
    }

    public static bool Prefix(string filename, Assembly assembly, ref Il2CppAssetBundle __result)
    {
        if (assembly == null)
        {
            __result = null!;
            return false;
        }

        var resourceName = assembly.GetName().Name + "." + filename.Replace("/", ".");
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            MelonLogger.Warning($"[SR2MP/Sr2eAssetBundleFix] Resource '{resourceName}' not found in {assembly.GetName().Name}");
            __result = null!;
            return false;
        }

        // Attempts so far:
        //   1. LoadFromMemory(byte[]): ObjectCollectedException — Il2CppInterop
        //      creates an internal Il2CppStructArray wrapper during marshaling
        //      that gets GC-collected before native code reads its Pointer.
        //      GC.KeepAlive on our managed wrapper doesn't help because the
        //      failing wrapper is the internal one.
        //   2. LoadFromFile(string): ICall LoadFromFile_Internal is NOT
        //      registered. Throws "ICall was not resolved".
        //   3. LoadFromStream(System.IO.Stream): signature wants
        //      Il2CppSystem.IO.Stream, no clean adapter.
        //
        // This attempt: LoadFromFileAsync IS registered (we see its ICall in
        // the startup log). Write the resource to a temp file, kick off the
        // async load, and synchronously wait for it. Async path takes a
        // different code path internally that doesn't go through the
        // Span<byte>/Il2CppStructArray marshaling that breaks LoadFromMemory.
        var bytes = new byte[stream.Length];
        var read = 0;
        while (read < bytes.Length)
        {
            var n = stream.Read(bytes, read, bytes.Length - read);
            if (n <= 0) break;
            read += n;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"sr2e_bundle_{Guid.NewGuid():N}.bundle");
        File.WriteAllBytes(tempPath, bytes);

        try
        {
            var req = Il2CppAssetBundleManager.LoadFromFileAsync(tempPath);
            if (req == null)
            {
                MelonLogger.Error($"[SR2MP/Sr2eAssetBundleFix] LoadFromFileAsync returned null request");
                __result = null!;
                return false;
            }

            // Block until the async load completes. AssetBundleCreateRequest
            // exposes assetBundle which itself blocks until done in Unity's
            // native code, so this is effectively a synchronous wait.
            __result = req.assetBundle;
        }
        catch (Exception e)
        {
            MelonLogger.Error($"[SR2MP/Sr2eAssetBundleFix] LoadFromFileAsync path threw: {e}");
            __result = null!;
            return false;
        }

        if (__result == null)
            MelonLogger.Warning($"[SR2MP/Sr2eAssetBundleFix] LoadFromFileAsync produced null bundle (temp={tempPath}, resource={resourceName})");
        else
            MelonLogger.Msg($"[SR2MP/Sr2eAssetBundleFix] Loaded SR2E bundle '{resourceName}' via LoadFromFileAsync + sync wait");

        return false;
    }
}

// Same problem on Il2CppAssetBundle.LoadAsset: the synchronous LoadAsset_Internal
// ICall is not registered (only the async variant is). SR2E calls this from
// SystemContextPatch line 55 in a foreach that loads every asset in the
// bundle, so without this redirect every menu prefab load fails.
//
// Strategy: target the bottom-most overload — IntPtr LoadAsset(string, IntPtr) —
// because the public Object LoadAsset(string) and generic LoadAsset<T>(string)
// both funnel into it. Replace the body with a call to the async variant
// (LoadAssetAsync_Internal IS registered) and block on .asset via the
// Il2CppAssetBundleRequest wrapper.
[HarmonyPatch]
internal static class Sr2eLoadAssetFix
{
    private static MethodBase? TargetMethod()
    {
        try
        {
            return typeof(Il2CppAssetBundle).GetMethod(
                nameof(Il2CppAssetBundle.LoadAsset),
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(IntPtr) },
                null);
        }
        catch
        {
            return null;
        }
    }

    public static bool Prefix(Il2CppAssetBundle __instance, string name, IntPtr typeptr, ref IntPtr __result)
    {
        try
        {
            var requestPtr = __instance.LoadAssetAsync(name, typeptr);
            if (requestPtr == IntPtr.Zero)
            {
                __result = IntPtr.Zero;
                return false;
            }

            var request = new Il2CppAssetBundleRequest(requestPtr);
            var asset = request.asset; // Unity blocks internally until done
            __result = asset?.Pointer ?? IntPtr.Zero;
        }
        catch (Exception e)
        {
            MelonLogger.Error($"[SR2MP/Sr2eLoadAssetFix] LoadAssetAsync redirect threw for '{name}': {e}");
            __result = IntPtr.Zero;
        }
        return false;
    }
}

// Il2CppAssetBundle.Contains delegates to UnityEngine.AssetBundle.Contains,
// which fails with the same "Method not found: GetPinnableReference()" error
// because the IL2CPP wrapper marshals the string parameter through a
// ReadOnlySpan<char> the wrapper API can't construct. SR2E calls Contains
// in SystemContextPatch line 108 to check whether each menu's prefab exists
// in the bundle; when it throws, the menu's `rootObject` stays null and the
// menu fails to register.
//
// Workaround: implement Contains by enumerating GetAllAssetNames() (which
// returns Il2CppStringArray and avoids the Span marshaling). String compare
// is case-insensitive to match Unity's documented behavior.
[HarmonyPatch]
internal static class Sr2eContainsFix
{
    private static MethodBase? TargetMethod()
    {
        try
        {
            return typeof(Il2CppAssetBundle).GetMethod(
                nameof(Il2CppAssetBundle.Contains),
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string) },
                null);
        }
        catch
        {
            return null;
        }
    }

    public static bool Prefix(Il2CppAssetBundle __instance, string name, ref bool __result)
    {
        try
        {
            var allNames = __instance.GetAllAssetNames();
            if (allNames == null)
            {
                __result = false;
                return false;
            }

            for (int i = 0; i < allNames.Length; i++)
            {
                if (string.Equals(allNames[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
        }
        catch (Exception e)
        {
            MelonLogger.Error($"[SR2MP/Sr2eContainsFix] Contains redirect threw for '{name}': {e}");
            __result = false;
        }
        return false;
    }
}


