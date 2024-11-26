using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches
{
	public class OfflineRaidSettingsMenuPatch_Override : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(RaidSettingsWindow), nameof(RaidSettingsWindow.Show));
		}

		private static RaidSettingsWindow instance;
		private static List<CanvasGroup> weatherCanvasGroups;
		private static bool randomWeather;
		public static bool UseCustomWeather
		{
			get
			{
				return randomWeather;
			}
			set
			{
				randomWeather = value;
			}
		}

		[PatchPostfix]
		private static void PatchPostfix(RaidSettingsWindow __instance, RaidSettings raidSettings, UiElementBlocker ____coopModeBlocker,
			List<CanvasGroup> ____weatherCanvasGroups, UpdatableToggle ____randomTimeToggle,
			UpdatableToggle ____randomWeatherToggle, List<CanvasGroup> ____waterAndFoodCanvasGroups,
			List<CanvasGroup> ____playersSpawnPlaceCanvasGroups, DropDownBox ____playersSpawnPlaceDropdown,
			List<CanvasGroup> ____timeCanvasGroups, DropDownBox ____timeFlowDropdown, UpdatableToggle ____coopModeToggle)
		{
			randomWeather = false;
			// Always disable the Coop Mode checkbox
			____coopModeBlocker.SetBlock(true, LocaleUtils.UI_FIKA_ALWAYS_COOP.Localized());
			____coopModeToggle.onValueChanged.RemoveAllListeners();
			____coopModeToggle.isOn = true;

			LocalizedText captionText = __instance.gameObject.transform.GetChild(0).GetChild(1).GetComponent<LocalizedText>();
			if (captionText != null)
			{
				captionText.method_2(LocaleUtils.UI_COOP_RAID_SETTINGS.Localized());
			}

			// Reset this one as otherwise it sticks
			raidSettings.TimeAndWeatherSettings.HourOfDay = -1;
			raidSettings.TimeAndWeatherSettings.TimeFlowType = ETimeFlowType.x1;

			if (____weatherCanvasGroups != null)
			{
				weatherCanvasGroups = ____weatherCanvasGroups;
			}

			foreach (CanvasGroup canvasGroup in ____waterAndFoodCanvasGroups)
			{
				canvasGroup.SetUnlockStatus(true, true);
			}

			foreach (CanvasGroup canvasGroup in ____playersSpawnPlaceCanvasGroups)
			{
				canvasGroup.SetUnlockStatus(true, true);
			}

			foreach (CanvasGroup canvasGroup in ____timeCanvasGroups)
			{
				canvasGroup.SetUnlockStatus(true, true);
			}

			// Remove redundant settings and add our own "Random" to make the setting clear, while also renaming index 0 to "Together"
			List<BaseDropDownBox.Struct1075> labelList = Traverse.Create(____playersSpawnPlaceDropdown).Field<List<BaseDropDownBox.Struct1075>>("list_0").Value;
			labelList.Clear();
			labelList.Add(new()
			{
				Label = "Together",
				Enabled = true
			});
			labelList.Add(new()
			{
				Label = "Random",
				Enabled = true
			});
			____playersSpawnPlaceDropdown.SetTextInternal("Together");

			instance = __instance;

			GameObject timeFlowDropDown = GameObject.Find("TimeFlowDropdown");
			if (timeFlowDropDown != null)
			{
				GameObject tooltip = timeFlowDropDown.transform.parent.GetChild(1).gameObject;
				tooltip.SetActive(false);

				CanvasGroup timeFlowCanvasGroup = timeFlowDropDown.GetComponent<CanvasGroup>();
				if (timeFlowCanvasGroup != null)
				{
					timeFlowCanvasGroup.SetUnlockStatus(true, true);
				}
				____timeFlowDropdown.Interactable = true;

				GameObject timeFlowText = GameObject.Find("TimeFlowText");
				if (timeFlowText != null)
				{
					CanvasGroup group = timeFlowText.GetComponent<CanvasGroup>();
					if (group != null)
					{
						group.SetUnlockStatus(true, true);
					}
				}
			}

			// If enforced from server, this will be true by default
			if (!raidSettings.TimeAndWeatherSettings.IsRandomWeather)
			{
				____randomWeatherToggle.Bind(ToggleWeather);
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

			randomWeather = enabled;
			instance.method_4();
		}
	}
}
