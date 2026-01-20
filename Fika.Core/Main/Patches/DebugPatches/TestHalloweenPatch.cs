using EFT;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

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
    public static void Prefix(HalloweenEventVisual __instance, bool ___bool_0, HalloweenVisualContainer ____container, Vector3[] positions)
    {
        if (__instance == null)
        {
            FikaGlobals.LogError("INSTANCE WAS NULL");
            return;
        }

        if (____container == null)
        {
            FikaGlobals.LogError("CONTAINER WAS NULL");
            return;
        }

        if (positions == null)
        {
            FikaGlobals.LogError("POSITIONS WAS NULL");
            return;
        }

        FikaGlobals.LogWarning($"Halloween Test Patch: transform: {__instance.transform + " " + __instance.transform.name}, bool: {___bool_0}, container: {____container}, positions: {positions}; {positions.Length}; {positions[0].ToStringHighResolution()}");
    }
}
