/*using Aki.Reflection.Patching;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.UI
{
    public class OfflineSettingsScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(MatchmakerOfflineRaidScreen).GetMethods().Where(x => x.Name == "Show" && x.GetParameters()[0].Name == "profileInfo").FirstOrDefault();

        [PatchPrefix]
        public static bool Prefix(MatchmakerOfflineRaidScreen __instance, InfoClass profileInfo, RaidSettings raidSettings,
            UpdatableToggle ____offlineModeToggle, DefaultUIButton ____changeSettingsButton, UiElementBlocker ____onlineBlocker, DefaultUIButton ____readyButton, DefaultUIButton ____nextButtonSpawner)
        {
            raidSettings.RaidMode = ERaidMode.Local;
            RemoveBlockers(__instance, profileInfo, raidSettings, ____offlineModeToggle, ____changeSettingsButton, ____onlineBlocker, ____readyButton, ____nextButtonSpawner);
            return true;
        }

        [PatchPostfix]
        public static void PatchPostfix(
           MatchmakerOfflineRaidScreen __instance, InfoClass profileInfo,
           RaidSettings raidSettings, UpdatableToggle ____offlineModeToggle,
           DefaultUIButton ____changeSettingsButton, UiElementBlocker ____onlineBlocker,
           DefaultUIButton ____readyButton, DefaultUIButton ____nextButtonSpawner)
        {
            var warningPanel = GameObject.Find("WarningPanelHorLayout");
            warningPanel?.SetActive(false);
            var settingslayoutcon = GameObject.Find("NonLayoutContainer");
            settingslayoutcon?.SetActive(false);
            var settingslist = GameObject.Find("RaidSettingsSummary");
            settingslist?.SetActive(false);
            RemoveBlockers(__instance, profileInfo, raidSettings, ____offlineModeToggle, ____changeSettingsButton, ____onlineBlocker, ____readyButton, ____nextButtonSpawner);

            ____changeSettingsButton?.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(null) { });
        }

        public static void RemoveBlockers(MatchmakerOfflineRaidScreen __instance, InfoClass profileInfo,
            RaidSettings raidSettings, UpdatableToggle ____offlineModeToggle,
            DefaultUIButton ____changeSettingsButton, UiElementBlocker ____onlineBlocker,
            DefaultUIButton ____readyButton, DefaultUIButton ____nextButtonSpawner)
        {
            raidSettings.RaidMode = ERaidMode.Local;
            raidSettings.BotSettings.BossType = EFT.Bots.EBossType.AsOnline;
            raidSettings.WavesSettings.IsBosses = true;
            raidSettings.WavesSettings.BotAmount = EFT.Bots.EBotAmount.Medium;

            ____onlineBlocker.RemoveBlock();
            ____onlineBlocker.enabled = false;
            ____offlineModeToggle.isOn = true;
            ____offlineModeToggle.enabled = false;
            ____offlineModeToggle.interactable = false;
            ____changeSettingsButton.Interactable = false;
            ____changeSettingsButton.enabled = false;
            ____readyButton.Interactable = false;
            ____readyButton.enabled = false;
        }
    }
}*/