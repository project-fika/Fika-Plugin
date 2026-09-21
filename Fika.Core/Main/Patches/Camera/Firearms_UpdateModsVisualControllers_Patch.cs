using EFT;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Reflection;
using SPTushonka.Reflection.Patching;

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
            __instance._tacticalComboVisualControllers = ToArray<TacticalComboVisualController>(__instance._weaponHierarchy);
            __instance._sightModVisualControllers = ToArray<SightModVisualControllers>(__instance._weaponHierarchy);
            __instance._launcherViauslControllers = ToArray<LauncherViauslController>(__instance._weaponHierarchy);

            var bipods = __instance._weaponHierarchy.GetComponentsInChildrenActiveIgnoreFirstLevel<BipodViewController>();
            __instance._bipodViewController = bipods.Count > 0 ? bipods[0] : null;

            return false;
        }

        return true;
    }
    
    private static Il2CppReferenceArray<T> ToArray<T>(Transform hierarchy) where T : UnityEngine.Component
    {
        return hierarchy.GetComponentsInChildrenActiveIgnoreFirstLevel<T>().ToArray().Cast<Il2CppReferenceArray<T>>();
    }
}
