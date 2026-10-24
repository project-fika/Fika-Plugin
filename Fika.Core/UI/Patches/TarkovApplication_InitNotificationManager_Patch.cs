using System.Reflection;
using System.Threading.Tasks;
using EFT;
using Fika.Core.Networking.Websocket;
using SPTushonka.Reflection.Il2Cpp;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches;

/// <summary>
/// The intention of this patch is to enable FikaNotificationManager after NotificationManager and the NotifierView are initialized.
/// </summary>
public class TarkovApplication_InitNotificationManager_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TarkovApplication)
            .GetMethod(nameof(TarkovApplication.EnsureNotificationManagerAsync));
    }

    [PatchPostfix]
    internal static void Postfix(Il2CppSystem.Threading.Tasks.Task __result)
    {
        _ = CreateWhenReady(__result);
    }

    private static async Task CreateWhenReady(Il2CppSystem.Threading.Tasks.Task setup)
    {
        await setup.AsManaged();
        if (!FikaNotificationManager.Exists)
        {
            FikaPlugin.Instance.NotificationManager = new();
        }
    }
}
