using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class CoopSettingsWindow_Show_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(CoopSettingsWindow).GetMethod(nameof(CoopSettingsWindow.Show));
		}

		[PatchPostfix]
		public static void Postfix(CoopSettingsWindow __instance)
		{
			LocalizedText localizedText = __instance.gameObject.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<LocalizedText>();
			if (localizedText != null)
			{
				localizedText.method_2(LocaleUtils.UI_COOP_RAID_SETTINGS.Localized());
			}
		}
	}
}
