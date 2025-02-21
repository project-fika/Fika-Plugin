using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Patches
{
    internal class Class1392_ReloadBackendLocale_Patch : ModulePatch
    {
        private static bool HasBeenSet = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Class1392).GetMethod(nameof(Class1392.ReloadBackendLocale));
        }

        [PatchPostfix]
        public static void Postfix(Task __result)
        {
            if (!HasBeenSet)
            {
                FikaPlugin.Instance.StartCoroutine(FikaPlugin.Instance.WaitForLocales(__result));
                HasBeenSet = true;
            }
        }
    }
}
