using Dissonance;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.VOIP
{
    public class DissonanceComms_Start_Patch : ModulePatch
    {
        public static bool IsReady;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(DissonanceComms), "Start");
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return IsReady;
        }
    }
}
