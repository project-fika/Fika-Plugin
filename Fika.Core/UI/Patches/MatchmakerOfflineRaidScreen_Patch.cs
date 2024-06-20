using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using System.Reflection;
using EFT;
using EFT.UI;

namespace Fika.Core.UI.Patches
{
    public class MatchmakerOfflineRaidScreen_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(MatchmakerOfflineRaidScreen).GetMethod("method_6");

        [PatchPrefix]
        private static bool PatchPrefix(MatchmakerOfflineRaidScreen __instance, RaidSettings ___raidSettings_0, UpdatableToggle ____offlineModeToggle, UiElementBlocker ____onlineBlocker)
        {
            // Force local
            ___raidSettings_0.RaidMode = ERaidMode.Local;
            
            // Default checkbox to be ticked
            ____offlineModeToggle.isOn = true;
            
            // Use default SPT message
            ____onlineBlocker.SetBlock(true, "Raids in SPT are always Offline raids. Don't worry - your progress will be saved!");
            
            // Skip method_5 since it updates raid settings
            __instance.method_8(true);

            // Skip all of method_6, since we're avoiding its side effects
            return false;
        }
    }
}