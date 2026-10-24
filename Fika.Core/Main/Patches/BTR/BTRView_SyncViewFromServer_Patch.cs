using System.Reflection;
using EFT.Vehicle;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.BTR;

public class BTRView_SyncViewFromServer_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BTRView).GetMethod(nameof(BTRView.SyncViewFromServer));
    }

    [PatchPrefix]
    public static void Prefix(BTRView __instance)
    {
        if (FikaBackendUtils.IsClient)
        {
            __instance._offlineGame = false;
        }
    }
}
