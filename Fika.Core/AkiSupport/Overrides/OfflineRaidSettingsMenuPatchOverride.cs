using Aki.Reflection.Patching;
using EFT.UI;
using EFT.UI.Matchmaker;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.AkiSupport.Overrides
{
    public class OfflineRaidSettingsMenuPatchOverride : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RaidSettingsWindow), nameof(RaidSettingsWindow.Show));
        }

        static RaidSettingsWindow instance;
        static List<CanvasGroup> weatherCanvasGroups;

        [PatchPostfix]
        private static void PatchPostfix(RaidSettingsWindow __instance, UiElementBlocker ____coopModeBlocker, List<CanvasGroup> ____weatherCanvasGroups,
            UpdatableToggle ____randomTimeToggle, UpdatableToggle ____randomWeatherToggle, List<CanvasGroup> ____waterAndFoodCanvasGroups)
        {
            // Always disable the Coop Mode checkbox
            ____coopModeBlocker.SetBlock(true, "Co-op is always enabled in Fika");

            if (____weatherCanvasGroups != null)
            {
                weatherCanvasGroups = ____weatherCanvasGroups;
            }

            foreach (CanvasGroup canvasGroup in ____waterAndFoodCanvasGroups)
            {
                canvasGroup.SetUnlockStatus(true, true);
            }

            instance = __instance;

            ____randomWeatherToggle.Bind(new Action<bool>(ToggleWeather));
            ____randomTimeToggle.gameObject.GetComponent<CanvasGroup>().SetUnlockStatus(false, false);

            GameObject weatherToggle = GameObject.Find("RandomWeatherCheckmark");
            if (weatherToggle != null)
            {
                CustomTextMeshProUGUI customTmp = weatherToggle.GetComponentInChildren<CustomTextMeshProUGUI>();
                if (customTmp != null)
                {
                    customTmp.text = "Use custom weather";
                }
            }
        }

        private static void ToggleWeather(bool enabled)
        {
            if (instance == null)
            {
                return;
            }

            if (weatherCanvasGroups == null)
            {
                return;
            }

            foreach (CanvasGroup item in weatherCanvasGroups)
            {
                item.SetUnlockStatus(enabled, enabled);
            }

            instance.method_4();
        }
    }
}
