using Aki.Reflection.Patching;
using EFT.UI;
using System.Reflection;

namespace Fika.Core.Coop.Matchmaker
{
    public class EnvironmentUIRoot_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EnvironmentUIRoot).GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EnvironmentUIRoot __instance)
        {
            MatchmakerAcceptPatches.EnvironmentUIRoot = __instance.gameObject;
        }
    }
}
