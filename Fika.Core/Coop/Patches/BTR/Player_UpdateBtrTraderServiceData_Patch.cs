using EFT;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Patches
{
	public class Player_UpdateBtrTraderServiceData_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(LocalPlayer).GetMethod(nameof(LocalPlayer.UpdateBtrTraderServiceData));
		}

		[PatchPrefix]
		public static bool Prefix(LocalPlayer __instance, ref Task __result)
		{
			if (FikaBackendUtils.IsClient)
			{
				__instance.InventoryController.GetTraderServicesDataFromServer(Profile.TraderInfo.TraderServiceToId[Profile.ETraderServiceSource.Btr]);
				__result = Task.CompletedTask;
				return false;
			}
			return true;
		}
	}
}
