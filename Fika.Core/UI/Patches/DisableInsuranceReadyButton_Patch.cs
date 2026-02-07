// © 2026 Lacyway All Rights Reserved

using System.Reflection;
using EFT.UI;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;

namespace Fika.Core.UI.Patches;

/// <summary>
/// Created by: Lacyway
/// </summary>
public class DisableInsuranceReadyButton_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchmakerInsuranceScreen).GetMethod(nameof(MatchmakerInsuranceScreen.Awake));
    }

    [PatchPostfix]
    static void Postfix(DefaultUIButton ____readyButton)
    {
        ____readyButton.SetDisabledTooltip("Disabled with Fika");
        ____readyButton.SetEnabledTooltip("Disabled with Fika");

        ____readyButton.Interactable = false;
    }
}