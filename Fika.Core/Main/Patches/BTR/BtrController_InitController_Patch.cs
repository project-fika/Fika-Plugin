using EFT.Vehicle;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.BTR;

internal class BtrController_InitController_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BtrController)
            .GetMethod(nameof(BtrController.InitController));
    }

    [PatchPrefix]
    public static bool Prefix(BtrController __instance, ref Task __result,
        ref GameWorld ____gameWorld, ref GlobalConfiguration.BTRGlobalSettings ____btrGlobalSettings)
    {
        if (FikaBackendUtils.IsServer)
        {
            return true;
        }

        if (Singleton<GlobalConfiguration>.Instance != null && Singleton<GlobalConfiguration>.Instance.BTRSettings != null)
        {
            ____btrGlobalSettings = Singleton<GlobalConfiguration>.Instance.BTRSettings;
        }
        else
        {
            Logger.LogError("GlobalConfiguration or BTRSettings was null!");
        }

        __result = __instance.InitClient();
        __instance.TransferItemsController = new BtrTransferItemsController(____gameWorld, ____btrGlobalSettings, true);
        if (FikaBackendUtils.IsClient)
        {
            __instance.TransferItemsController.InitItemControllerServer("656f0f98d80a697f855d34b1", "BTR");
        }
        return false;
    }
}
