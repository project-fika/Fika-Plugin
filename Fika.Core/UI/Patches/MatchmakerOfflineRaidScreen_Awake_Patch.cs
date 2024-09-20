using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Utils;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class MatchmakerOfflineRaidScreen_Show_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MatchmakerOfflineRaidScreen).GetMethods().FirstOrDefault(x => x.Name == "Show" && x.GetParameters().Length == 3);
		}

		[PatchPostfix]
		public static void Postfix(MatchmakerOfflineRaidScreen __instance)
		{
			LocalizedText captionText = __instance.gameObject.transform.GetChild(2).GetChild(0).GetComponent<LocalizedText>();
			if (captionText != null)
			{
				captionText.method_2(LocaleUtils.UI_COOP_GAME_MODE.Localized());
			}

			LocalizedText descriptionText = __instance.gameObject.transform.GetChild(1).GetChild(1).GetComponent<LocalizedText>();
			if (descriptionText != null)
			{
				descriptionText.method_2(LocaleUtils.UI_RAID_SETTINGS_DESCRIPTION.Localized());
			}
		}
	}
}
