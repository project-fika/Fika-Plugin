using Comfort.Common;
using EFT;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Patches
{
	internal class BTRControllerClass_Init_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BTRControllerClass).GetMethod(nameof(BTRControllerClass.method_0));
		}

		[PatchPrefix]
		public static bool Prefix(BTRControllerClass __instance, ref CancellationToken cancellationToken, ref Task __result, ref GameWorld ___gameWorld_0, ref BackendConfigSettingsClass.BTRGlobalSettings ___btrglobalSettings_0, ref bool ___bool_1)
		{
			___btrglobalSettings_0 = Singleton<BackendConfigSettingsClass>.Instance.BTRSettings;

			if (FikaBackendUtils.IsServer)
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

			__instance.TransferItemsController = new GClass1637(___gameWorld_0, ___btrglobalSettings_0, true);
			if (FikaBackendUtils.IsClient)
			{
				__instance.TransferItemsController.InitItemControllerServer("656f0f98d80a697f855d34b1", "BTR");

			}

			__result = Task.CompletedTask;
			return false;
		}
	}
}
