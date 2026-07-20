using EFT.Communications;
using System.Reflection;
using EFT;
using Fika.Core.Networking.Websocket;
using SPT.Reflection.Patching;

namespace Fika.Core.UI.Patches;

/// <summary>
/// The intention of this patch is to enable FikaNotificationManager after NotificationManager and the NotifierView are initialized.
/// </summary>
public class TarkovApplication_InitNotificationManager_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TarkovApplication)
            .GetMethod(nameof(TarkovApplication.InitNotificationManager));
    }

    [PatchPostfix]
    internal static void Postfix()
    {
        if (!FikaNotificationManager.Exists)
        {
            FikaPlugin.Instance.NotificationManager = new();
        }
    }
}
