using System.Reflection;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches;

public class CoopSettingsWindow_Show_Patch : ModulePatch
{
    // 1.1 inlines CoopSettingsWindow.Show into its only caller
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchMakerAcceptScreen).GetMethod(nameof(MatchMakerAcceptScreen.OpenCoopSettingsWindow));
    }

    [PatchPostfix]
    public static void Postfix(MatchMakerAcceptScreen __instance)
    {
        var window = __instance._coopSettingsWindow;
        if (window == null)
        {
            return;
        }

        var localizedText = FindText(window.gameObject.transform);
        if (localizedText != null)
        {
            localizedText.SetLabelText(LocaleUtils.UI_COOP_RAID_SETTINGS.Localized());
        }
    }

    private static LocalizedText FindText(Transform root)
    {
        if (root.childCount == 0 || root.GetChild(0).childCount == 0)
        {
            return null;
        }

        var parent = root.GetChild(0).GetChild(0);
        return parent.childCount > 1 ? parent.GetChild(1).GetComponent<LocalizedText>() : null;
    }
}
