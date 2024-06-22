// © 2024 Lacyway All Rights Reserved

using EFT.UI;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI
{
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
        static void Postfix(DefaultUIButton ____readyButton)
        {
            ____readyButton.SetDisabledTooltip("Disabled with Fika");
            ____readyButton.SetEnabledTooltip("Disabled with Fika");

            ____readyButton.Interactable = false;
        }
    }
}