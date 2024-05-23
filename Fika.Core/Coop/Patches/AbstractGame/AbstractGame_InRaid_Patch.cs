using Aki.Reflection.Patching;
using EFT;
using Fika.Core.Coop.GameMode;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    /// <summary>
    /// Used to support mods that rely on the <see cref="AbstractGame.InRaid"/> property, which normally casts to <see cref="EFT.LocalGame"/>
    /// </summary>
    internal class AbstractGame_InRaid_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AbstractGame).GetProperty(nameof(AbstractGame.InRaid)).GetGetMethod();
        }

        [PatchPrefix]
        private static bool PreFix(AbstractGame __instance, ref bool __result)
        {
            __result = __instance is CoopGame;
            return false;
        }
    }
}
