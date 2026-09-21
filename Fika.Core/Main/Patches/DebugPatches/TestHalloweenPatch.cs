using System.Reflection;
using EFT;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.DebugPatches;

[DebugPatch]
public class TestHalloweenPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HalloweenEventVisual)
            .GetMethod(nameof(HalloweenEventVisual.Initialize));
    }

    [PatchPrefix]
    public static void Prefix(HalloweenEventVisual __instance, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> positions)
    {
        if (__instance == null)
        {
            FikaGlobals.LogError("INSTANCE WAS NULL");
            return;
        }

        if (__instance._container == null)
        {
            FikaGlobals.LogError("CONTAINER WAS NULL");
            return;
        }

        if (positions == null)
        {
            FikaGlobals.LogError("POSITIONS WAS NULL");
            return;
        }

        FikaGlobals.LogWarning($"Halloween Test Patch: transform: {__instance.transform + " " + __instance.transform.name}, bool: {__instance._isInitialized}, container: {__instance._container}, positions: {positions}; {positions.Length}; {positions[0].ToStringHighResolution()}");
    }
}
