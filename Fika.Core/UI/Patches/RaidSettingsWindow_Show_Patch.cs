using EFT.UI.Matchmaker;
using Fika.Core.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class RaidSettingsWindow_Show_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(RaidSettingsWindow).GetMethod(nameof(RaidSettingsWindow.Show));
		}

		[PatchPrefix]
		public static bool Prefix()
		{
			if (!FikaPlugin.Instance.CanEditRaidSettings)
			{
				NotificationManagerClass.DisplayMessageNotification(LocaleUtils.UI_NOTIFICATION_RAIDSETTINGS_DISABLED.Localized(), iconType: EFT.Communications.ENotificationIconType.Alert);
				return false;
			}

			return true;
		}
	}
}
