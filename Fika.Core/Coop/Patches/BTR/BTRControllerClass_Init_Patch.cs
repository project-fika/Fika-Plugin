using Comfort.Common;
using EFT;
using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Patches
{
    internal class BTRControllerClass_Init_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BTRControllerClass).GetMethod(nameof(BTRControllerClass.method_0));
        }

        [PatchPrefix]
        public static bool Prefix(BTRControllerClass __instance, ref CancellationToken cancellationToken, ref Task __result, ref GameWorld ___GameWorld_0, ref BackendConfigSettingsClass.BTRGlobalSettings ___BtrglobalSettings_0, ref bool ___Bool_1)
        {
            if (FikaBackendUtils.IsServer)
            {
                return true;
            }

            if (Singleton<BackendConfigSettingsClass>.Instance != null && Singleton<BackendConfigSettingsClass>.Instance.BTRSettings != null)
            {
                ___BtrglobalSettings_0 = Singleton<BackendConfigSettingsClass>.Instance.BTRSettings;
            }
            else
            {
                Logger.LogError("BackendConfigSettingsClass or BTRSettings was null!");
            }

            __result = __instance.method_5();
            __instance.TransferItemsController = new BTRTransferItemsControllerClass(___GameWorld_0, ___BtrglobalSettings_0, true);
            if (FikaBackendUtils.IsClient)
            {
                __instance.TransferItemsController.InitItemControllerServer("656f0f98d80a697f855d34b1", "BTR");
            }
            return false;

            /*if (FikaBackendUtils.IsServer)
            {
                ___bool_1 = true;
                BTRControllerClass.Instance.method_1(cancellationToken);
                BTRControllerClass.Instance.method_5();
            }
            else
            {
                ___bool_1 = false;
                BTRControllerClass.Instance.method_5();
            }

            __instance.TransferItemsController = new GClass1671(___gameWorld_0, ___btrglobalSettings_0, true);
            if (FikaBackendUtils.IsClient)
            {
                __instance.TransferItemsController.InitItemControllerServer("656f0f98d80a697f855d34b1", "BTR");
            }

            __result = Task.CompletedTask;
            return false;*/
        }
    }
}
