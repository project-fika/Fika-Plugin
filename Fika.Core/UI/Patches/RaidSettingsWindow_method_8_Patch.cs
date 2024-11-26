using EFT.UI;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.UI.Patches
{
	/// <summary>
	/// This allows the user to modify all AI settings even after modifying AI amount / difficulty
	/// </summary>
	public class RaidSettingsWindow_method_8_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(RaidSettingsWindow).GetMethod(nameof(RaidSettingsWindow.method_8));
		}

		[PatchPrefix]
		public static bool Prefix(CanvasGroup ____aiDifficultyCanvasGroup, CanvasGroup ____aiAmountCanvasGroup, DropDownBox ____aiAmountDropdown, List<CanvasGroup> ____wavesCanvasGroups)
		{
			bool hasAi = ____aiAmountDropdown.CurrentIndex != 1;
			foreach (CanvasGroup canvasGroup in ____wavesCanvasGroups)
			{
				canvasGroup.interactable = hasAi;
				canvasGroup.blocksRaycasts = hasAi;
				canvasGroup.alpha = hasAi ? 1f : 0.3f;
			}

			____aiDifficultyCanvasGroup.SetUnlockStatus(hasAi, true);
			____aiAmountCanvasGroup.SetUnlockStatus(true, true);

			return false;
		}
	}
}
