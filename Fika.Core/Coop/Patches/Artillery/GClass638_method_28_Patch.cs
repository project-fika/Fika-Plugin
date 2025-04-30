using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass638_method_28_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass638).GetMethod(nameof(GClass638.method_28));
        }

        [PatchPrefix]
        public static void Prefix(GClass638 __instance, GClass1439 serverProjectile)
        {
            __instance.method_31(serverProjectile);
        }
    }
}
