using Dissonance;
using EFT;
using Fika.Core.Patching;
using HarmonyLib;
using System.Reflection;

namespace Fika.Core.Main.Patches.VOIP
{
    class Player_IDissonancePlayerType_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Player), "Dissonance.IDissonancePlayer.Type");
        }

        [PatchPrefix]
        public static bool Prefix(Player __instance, ref NetworkPlayerType __result)
        {
            __result = __instance.IsYourPlayer ? NetworkPlayerType.Local : NetworkPlayerType.Remote;
            return false;
        }
    }
}
