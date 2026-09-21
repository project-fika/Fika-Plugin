// © 2026 Lacyway All Rights Reserved

using System.Reflection;
using EFT.UI;
using EFT.UI.Matchmaker;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches;

/// <summary>
/// Created by: Lacyway
/// </summary>
public class DisableMatchSettingsReadyButton_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchmakerOfflineRaidScreen).GetMethod(nameof(MatchmakerOfflineRaidScreen.Awake));
    }

    [PatchPostfix]
    static void Postfix(EFT.UI.Matchmaker.MatchmakerOfflineRaidScreen __instance)
    {
        __instance._readyButton.SetDisabledTooltip("Disabled with Fika");
        __instance._readyButton.SetEnabledTooltip("Disabled with Fika");

        __instance._readyButton.Interactable = false;
    }
}