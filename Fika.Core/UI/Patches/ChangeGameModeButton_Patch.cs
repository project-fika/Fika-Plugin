using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches;

public class ChangeGameModeButton_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ChangeGameModeButton).GetMethod(nameof(ChangeGameModeButton.Show));
    }

    [PatchPrefix]
    private static bool PrefixChange(TextMeshProUGUI ____buttonLabel, TextMeshProUGUI ____buttonDescription, Image ____buttonDescriptionIcon,
        GameObject ____availableState)
    {
        ____buttonLabel.SetText("PvE");
        ____buttonDescription.SetText($"Fika will always be {ColorizeText(EColor.BLUE, "PvE")}");
        ____buttonDescriptionIcon.gameObject.SetActive(false);
        ____availableState.SetActive(true);
        return false;
    }
}