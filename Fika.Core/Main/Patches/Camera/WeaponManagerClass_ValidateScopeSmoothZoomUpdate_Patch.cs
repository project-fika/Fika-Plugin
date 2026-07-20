using EFT;
using System.Reflection;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Camera;

public class WeaponManagerClass_ValidateScopeSmoothZoomUpdate_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Firearms).GetMethod(nameof(Firearms.ValidateScopeSmoothZoomUpdate));
    }

    [PatchPrefix]
    public static bool Prefix(Firearms __instance)
    {
        if (__instance.Player != null && !__instance.Player.IsYourPlayer)
        {
            return false;
        }
        return true;
    }
}
