using Fika.Core.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class WeaponManagerClass_method_12_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WeaponManagerClass).GetMethod(nameof(WeaponManagerClass.method_12));
        }

        [PatchPrefix]
        public static bool Prefix(WeaponManagerClass __instance)
        {
            if (__instance.Player != null && !__instance.Player.IsYourPlayer)
            {
                __instance.TacticalComboVisualController_0 = [.. __instance.Transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<TacticalComboVisualController>()];
                __instance.SightModVisualControllers_0 = [.. __instance.Transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<SightModVisualControllers>()];
                __instance.LauncherViauslController_0 = [.. __instance.Transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<LauncherViauslController>()];
                __instance.BipodViewController_0 = __instance.Transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<BipodViewController>().FirstOrDefault();

                return false;
            }

            return true;
        }
    }
}
