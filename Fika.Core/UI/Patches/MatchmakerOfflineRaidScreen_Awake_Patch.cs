using System.Linq;
using System.Reflection;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches;

public sealed class MatchmakerOfflineRaidScreen_Show_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchmakerOfflineRaidScreen)
            .GetMethods()
            .FirstOrDefault(x => x.Name == "Show" && x.GetParameters().Length == 3);
    }

    [PatchPostfix]
    public static void Postfix(MatchmakerOfflineRaidScreen __instance)
    {
        var captionText = FindText(__instance.gameObject.transform, 2, 0);
        if (captionText != null)
        {
            captionText.SetLabelText(LocaleUtils.UI_COOP_GAME_MODE.Localized());
        }

        var descriptionText = FindText(__instance.gameObject.transform, 1, 1);
        if (descriptionText != null)
        {
            descriptionText.SetLabelText(LocaleUtils.UI_RAID_SETTINGS_DESCRIPTION.Localized());
        }
    }

    private static LocalizedText FindText(Transform root, int first, int second)
    {
        if (root.childCount <= first)
        {
            return null;
        }

        var child = root.GetChild(first);
        return child.childCount > second ? child.GetChild(second).GetComponent<LocalizedText>() : null;
    }
}
