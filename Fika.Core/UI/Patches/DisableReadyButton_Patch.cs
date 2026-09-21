// © 2026 Lacyway All Rights Reserved

using System.Reflection;
using EFT.UI;
using EFT.UI.Matchmaker;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches;

/// <summary>
/// Created by: Lacyway
/// </summary>
public class DisableReadyButton_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchMakerSelectionLocationScreen).GetMethod(nameof(MatchMakerSelectionLocationScreen.Awake));
    }

    [PatchPostfix]
    static void Postfix(EFT.UI.Matchmaker.MatchMakerSelectionLocationScreen __instance)
    {
        __instance._readyButton.SetDisabledTooltip("Disabled with Fika");
        __instance._readyButton.SetEnabledTooltip("Disabled with Fika");

        __instance._readyButton.Interactable = false;
    }
}