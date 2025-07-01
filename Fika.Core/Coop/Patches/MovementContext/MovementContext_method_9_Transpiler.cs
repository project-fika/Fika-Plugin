using EFT;
using Fika.Core.Patching;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    /// <summary>
    /// Stops unnecessary static lookups to <see cref="BackendConfigAbstractClass.Config"/>
    /// </summary>
    internal class MovementContext_method_9_Transpiler : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementContext)
                .GetMethod(nameof(MovementContext.method_9));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] inst = [.. instructions];
            yield return inst[14];
            yield return inst[15];
            yield return inst[16];
            yield return inst[17];
            yield return inst[18];
            yield return inst[19];
        }
    }
}
