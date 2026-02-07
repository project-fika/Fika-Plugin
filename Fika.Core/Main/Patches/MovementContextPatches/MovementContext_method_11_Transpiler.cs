using System.Collections.Generic;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.MovementContextPatches;

/// <summary>
/// Stops unnecessary static lookups to <see cref="BackendConfigAbstractClass.Config"/>
/// </summary>
internal class MovementContext_method_11_Transpiler : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MovementContext)
            .GetMethod(nameof(MovementContext.method_11));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        CodeInstruction[] inst = [.. instructions];
        yield return inst[14];
        yield return inst[15];
        yield return inst[16];
        yield return inst[17];
    }
}
