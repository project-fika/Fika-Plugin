using System.Linq;
using System.Reflection;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

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
        var captionText = __instance.gameObject.transform.GetChild(2).GetChild(0).GetComponent<LocalizedText>();
        if (captionText != null)
        {
            captionText.SetLabelText(LocaleUtils.UI_COOP_GAME_MODE.Localized());
        }

        var descriptionText = __instance.gameObject.transform.GetChild(1).GetChild(1).GetComponent<LocalizedText>();
        if (descriptionText != null)
        {
            descriptionText.SetLabelText(LocaleUtils.UI_RAID_SETTINGS_DESCRIPTION.Localized());
        }
    }
}
