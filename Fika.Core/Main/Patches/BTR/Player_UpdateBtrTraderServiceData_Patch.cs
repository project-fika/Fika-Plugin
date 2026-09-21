using System.Reflection;
using System.Threading.Tasks;
using EFT;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.BTR;

public class Player_UpdateBtrTraderServiceData_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LocalPlayer).GetMethod(nameof(LocalPlayer.UpdateBtrTraderServiceData));
    }

    [PatchPrefix]
    public static bool Prefix(LocalPlayer __instance, ref Il2CppSystem.Threading.Tasks.Task __result)
    {
        if (FikaBackendUtils.IsClient)
        {
            __instance.InventoryController.GetTraderServicesDataFromServer(Profile.TraderInfo.BTR_TRADER_ID);
            __result = Il2CppSystem.Threading.Tasks.Task.CompletedTask;
            return false;
        }
        return true;
    }
}
