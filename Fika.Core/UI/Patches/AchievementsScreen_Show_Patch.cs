using EFT.UI;
using Fika.Core.Patching;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.UI.Patches
{
    /// <summary>
    /// By default the amount of players that have earned an achievement is not shown in if the session mode is <see cref="ESessionMode.Pve"/>, this patch forces it to be shown regardless of mode
    /// </summary>
    public class AchievementsScreen_Show_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AchievementsScreen).GetMethod(nameof(AchievementsScreen.Show));
        }

        [PatchPostfix]
        public static void Postfix(CanvasGroup ____allPlayersPercentCanvasGroup)
        {
            ____allPlayersPercentCanvasGroup.alpha = 1f;
            ____allPlayersPercentCanvasGroup.interactable = true;
        }
    }
}
