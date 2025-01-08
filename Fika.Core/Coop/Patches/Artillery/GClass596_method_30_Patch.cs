using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass619_method_30_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass619).GetMethod(nameof(GClass619.method_30));
        }

        [PatchPrefix]
        public static void Prefix(GClass619 __instance, GClass1390 serverProjectile)
        {
            __instance.method_31(serverProjectile);
        }
    }
}
