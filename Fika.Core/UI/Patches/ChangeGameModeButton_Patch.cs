using EFT.UI;
using Fika.Core.Utils;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Fika.Core.Utils.ColorUtils;

namespace Fika.Core.UI.Patches
{
	public class ChangeGameModeButton_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(ChangeGameModeButton).GetMethod(nameof(ChangeGameModeButton.Show));
		}

		[PatchPrefix]
		private static bool PrefixChange(TextMeshProUGUI ____buttonLabel, TextMeshProUGUI ____buttonDescription, Image ____buttonDescriptionIcon,
			GameObject ____availableState)
		{
			____buttonLabel.text = "PvE";
			____buttonDescription.text = $"Fika will always be {ColorizeText(Colors.BLUE, "PvE")}";
			____buttonDescriptionIcon.gameObject.SetActive(false);
			____availableState.SetActive(true);
			return false;
		}
	}
}