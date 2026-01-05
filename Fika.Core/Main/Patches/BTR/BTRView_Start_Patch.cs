using EFT.GlobalEvents;
using EFT.Vehicle;
using Fika.Core.Main.Components;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.BTR;

public class BTRView_Start_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BTRView)
            .GetMethod(nameof(BTRView.Start));
    }

    [PatchPostfix]
    public static void Postfix(BTRView __instance)
    {
        if (FikaBackendUtils.IsServer)
        {
            BTRViewSynchronizer.CreateInstance(__instance);
        }
        else
        {
            __instance.moveLerpValue = 0.18f;
        }
    }
}
