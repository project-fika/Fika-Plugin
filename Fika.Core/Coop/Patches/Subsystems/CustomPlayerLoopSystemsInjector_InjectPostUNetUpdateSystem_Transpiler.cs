using CustomPlayerLoopSystem;
using Fika.Core.Patching;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Coop.Patches.Subsystems
{
    /// <summary>
    /// Prevents logic that is never used from being injected into the <see cref="UnityEngine.LowLevel.PlayerLoop"/>
    /// </summary>
    internal class CustomPlayerLoopSystemsInjector_InjectPostUNetUpdateSystem_Transpiler : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(CustomPlayerLoopSystemsInjector).GetMethod(nameof(CustomPlayerLoopSystemsInjector.InjectPostUNetUpdateSystem));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile()
        {
            yield return new(OpCodes.Ret);
        }
    }
}
