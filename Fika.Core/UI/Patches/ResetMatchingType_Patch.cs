using System.Reflection;
using EFT;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.EssentialPatches
{
    /// <summary>
    /// Reset MatchingType to Single when the game ends.
    /// </summary>
    public class ResetMatchingType_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnDestroy),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void Postfix(GameWorld __instance)
        {
            FikaBackendUtils.MatchingType = EMatchmakerType.Single;
        }
    }
}