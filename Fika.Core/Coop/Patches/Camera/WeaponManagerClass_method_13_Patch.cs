using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class WeaponManagerClass_method_13_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WeaponManagerClass).GetMethod(nameof(WeaponManagerClass.method_13));
        }

        [PatchPrefix]
        public static bool Prefix(WeaponManagerClass __instance)
        {
            if (__instance.Player != null && !__instance.Player.IsYourPlayer)
            {
                __instance.tacticalComboVisualController_0 = [.. __instance.transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<TacticalComboVisualController>()];
                __instance.sightModVisualControllers_0 = [.. __instance.transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<SightModVisualControllers>()];
                __instance.launcherViauslController_0 = [.. __instance.transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<LauncherViauslController>()];
                __instance.bipodViewController_0 = __instance.transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<BipodViewController>().FirstOrDefault();

                return false;
            }

            return true;
        }
    }
}
