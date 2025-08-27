using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Main.Patches.BTR;

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
            __instance.InventoryController.GetTraderServicesDataFromServer(Profile.TraderInfo.BTR_TRADER_ID);
            __result = Task.CompletedTask;
            return false;
        }
        return true;
    }
}
