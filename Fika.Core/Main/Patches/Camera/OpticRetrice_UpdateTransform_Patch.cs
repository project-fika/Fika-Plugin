using System.Reflection;
using EFT.CameraControl;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Camera;

public class OpticRetrice_UpdateTransform_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(OpticRetrice).GetMethod(nameof(OpticRetrice.UpdateTransform));
    }

    [PatchPrefix]
    public static bool Prefix(EFT.CameraControl.OpticRetrice __instance, OpticSight opticSight)
    {
        return opticSight.ScopeData != null && opticSight.ScopeData.Reticle != null && __instance._renderer != null;
    }
}
