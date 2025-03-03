using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass622_method_28_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass622).GetMethod(nameof(GClass622.method_28));
        }

        [PatchPrefix]
        public static void Prefix(GClass622 __instance, GClass1395 serverProjectile)
        {
            __instance.method_31(serverProjectile);
        }
    }
}
