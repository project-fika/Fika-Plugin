using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches.Overrides
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
			get => randomWeather;
			set => randomWeather = value;
		}

		[PatchPostfix]
		private static void PatchPostfix(RaidSettingsWindow __instance, UiElementBlocker ____coopModeBlocker,
			List<CanvasGroup> ____weatherCanvasGroups, UpdatableToggle ____randomTimeToggle,
			UpdatableToggle ____randomWeatherToggle, List<CanvasGroup> ____waterAndFoodCanvasGroups,
			List<CanvasGroup> ____playersSpawnPlaceCanvasGroups, DropDownBox ____playersSpawnPlaceDropdown,
			RaidSettings raidSettings)
		{
			randomWeather = false;
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

			foreach (CanvasGroup canvasGroup in ____playersSpawnPlaceCanvasGroups)
			{
				canvasGroup.SetUnlockStatus(true, true);
			}

			// Remove redundant settings and add our own "Random" to make the setting clear, while also renaming index 0 to "Together"
			List<BaseDropDownBox.Struct969> labelList = Traverse.Create(____playersSpawnPlaceDropdown).Field<List<BaseDropDownBox.Struct969>>("list_0").Value;
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

			// If enforced from server, this will be true by default
			if (!raidSettings.TimeAndWeatherSettings.IsRandomWeather)
			{
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
