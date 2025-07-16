using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Main.Patches
{
    public class Player_UpdateBtrTraderServiceData_Patch : FikaPatch
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
