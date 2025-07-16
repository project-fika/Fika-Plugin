using EFT;
using Fika.Core.Patching;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    /// <summary>
    /// Stops unnecessary static lookups to <see cref="BackendConfigAbstractClass.Config"/>
    /// </summary>
    internal class MovementContext_RestorePreviousYaw_Transpiler : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementContext)
                .GetMethod(nameof(MovementContext.RestorePreviousYaw));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] inst = [.. instructions];            
            yield return inst[19];
            yield return inst[20];
            yield return inst[21];
            yield return inst[22];
            yield return inst[23];
            yield return inst[24];
            yield return inst[25];
            yield return inst[26];
            yield return inst[27];
        }
    }
}
