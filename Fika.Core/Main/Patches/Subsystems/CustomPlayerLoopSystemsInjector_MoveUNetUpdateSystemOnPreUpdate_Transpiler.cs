using CustomPlayerLoopSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Main.Patches.Subsystems;

/// <summary>
/// Prevents logic that is never used from being injected into the <see cref="UnityEngine.LowLevel.PlayerLoop"/>
/// </summary>
internal class CustomPlayerLoopSystemsInjector_MoveUNetUpdateSystemOnPreUpdate_Transpiler : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(CustomPlayerLoopSystemsInjector).GetMethod(nameof(CustomPlayerLoopSystemsInjector.MoveUNetUpdateSystemOnPreUpdate));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile()
    {
        yield return new(OpCodes.Ret);
    }
}
