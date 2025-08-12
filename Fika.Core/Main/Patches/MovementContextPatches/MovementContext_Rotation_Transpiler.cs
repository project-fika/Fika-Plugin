using EFT;
using Fika.Core.Patching;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.Main.Patches.MovementContextPatches;

/// <summary>
/// Stops unnecessary static lookups to <see cref="BackendConfigAbstractClass.Config"/>
/// </summary>
internal class MovementContext_Rotation_Transpiler : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MovementContext)
            .GetProperty(nameof(MovementContext.Rotation))
            .GetGetMethod();
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        CodeInstruction[] inst = [.. instructions];
        yield return inst[13];
        yield return inst[14];
        yield return inst[15];
    }
}
