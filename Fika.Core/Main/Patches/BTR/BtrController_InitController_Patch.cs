using EFT.Vehicle;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.BTR;

internal class BtrController_InitController_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BtrController)
            .GetMethod(nameof(BtrController.InitController));
    }

    [PatchPrefix]
    public static bool Prefix(BtrController __instance, ref Il2CppSystem.Threading.Tasks.Task __result)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        if (FikaBackendUtils.IsServer)
        {
            return true;
        }

        if (Singleton<GlobalConfiguration>.Instance != null && Singleton<GlobalConfiguration>.Instance.BTRSettings != null)
        {
            __instance._btrGlobalSettings = Singleton<GlobalConfiguration>.Instance.BTRSettings;
        }
        else
        {
            Logger.LogError("GlobalConfiguration or BTRSettings was null!");
        }

        __result = __instance.InitClient();
        __instance.TransferItemsController = new BtrTransferItemsController(__instance._gameWorld, __instance._btrGlobalSettings, true);
        if (FikaBackendUtils.IsClient)
        {
            __instance.TransferItemsController.InitItemControllerServer("656f0f98d80a697f855d34b1", "BTR");
        }
        return false;
    }
}
