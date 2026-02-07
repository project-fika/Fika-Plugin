using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.DebugPatches;

/// <summary>
/// Removes the need for a Lab Keycard on debug builds
/// </summary>
[DebugPatch]
internal class LabsKeycardDebugPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MainMenuControllerClass)
            .GetMethod(nameof(MainMenuControllerClass.method_53));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile()
    {
        yield return new(OpCodes.Ldc_I4_1);
        yield return new(OpCodes.Ret);
    }
}
