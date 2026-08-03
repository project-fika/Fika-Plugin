using System.Reflection;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;
using EFT;
using EFT.Communications;

namespace Fika.Core.UI.Patches;

public class RaidSettingsWindow_Show_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RaidSettingsWindow)
            .GetMethod(nameof(RaidSettingsWindow.Show));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        if (!FikaPlugin.Instance.Settings.CanEditRaidSettings)
        {
            NotificationManager.DisplayMessageNotification(LocaleUtils.UI_NOTIFICATION_RAIDSETTINGS_DISABLED.Localized(), iconType: ENotificationIconType.Alert);
            return false;
        }

        return true;
    }
}
