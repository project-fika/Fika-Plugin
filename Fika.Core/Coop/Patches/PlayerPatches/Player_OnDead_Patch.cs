using EFT;
using Fika.Core.Patching;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Coop.Patches
{
    /// <summary>
    /// This patch stops BSGs dogtag handling as it is poorly executed
    /// </summary>
    public class Player_OnDead_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            //Check for gclass increments
            return typeof(LocalPlayer).GetMethod(nameof(LocalPlayer.OnDead));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile()
        {

            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, typeof(Player).GetMethod(nameof(Player.OnDead)));
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
