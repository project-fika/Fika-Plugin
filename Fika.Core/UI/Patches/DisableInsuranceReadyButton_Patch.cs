// © 2025 Lacyway All Rights Reserved

using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches;

/// <summary>
/// Created by: Lacyway
/// </summary>
public class DisableInsuranceReadyButton_Patch : FikaPatch
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