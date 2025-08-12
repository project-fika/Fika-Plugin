using Fika.Core.Patching;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Main.Patches.Debug;

/// <summary>
/// Removes the need for a Lab Keycard on debug builds
/// </summary>
[DebugPatch]
internal class LabsKeycardDebugPatch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MainMenuControllerClass)
            .GetMethod(nameof(MainMenuControllerClass.method_52));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile()
    {
        yield return new(OpCodes.Ldc_I4_1);
        yield return new(OpCodes.Ret);
    }
}
