using EFT;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Camera;

public class Firearms_UpdateModsVisualControllers_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Firearms).GetMethod(nameof(Firearms.UpdateModsVisualControllers));
    }

    [PatchPrefix]
    public static bool Prefix(Firearms __instance)
    {
        if (__instance.Player != null && !__instance.Player.IsYourPlayer)
        {
            __instance._tacticalComboVisualControllers = [.. __instance._weaponHierarchy.GetComponentsInChildrenActiveIgnoreFirstLevel<TacticalComboVisualController>()];
            __instance._sightModVisualControllers = [.. __instance._weaponHierarchy.GetComponentsInChildrenActiveIgnoreFirstLevel<SightModVisualControllers>()];
            __instance._launcherViauslControllers = [.. __instance._weaponHierarchy.GetComponentsInChildrenActiveIgnoreFirstLevel<LauncherViauslController>()];
            __instance._bipodViewController = __instance._weaponHierarchy.GetComponentsInChildrenActiveIgnoreFirstLevel<BipodViewController>().FirstOrDefault();

            return false;
        }

        return true;
    }
}
