using System.Reflection;
using EFT.UI;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches;

/// <summary>
/// By default the amount of players that have earned an achievement is not shown in if the session mode is <see cref="ESessionMode.Pve"/>, this patch forces it to be shown regardless of mode
/// </summary>
public class AchievementsScreen_Show_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(AchievementsScreen).GetMethod(nameof(AchievementsScreen.Show));
    }

    [PatchPostfix]
    public static void Postfix(EFT.UI.AchievementsScreen __instance)
    {
        __instance._allPlayersPercentCanvasGroup.alpha = 1f;
        __instance._allPlayersPercentCanvasGroup.interactable = true;
    }
}
