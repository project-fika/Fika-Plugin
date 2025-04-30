using Fika.Core.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Patches
{
    internal class Class1401_ReloadBackendLocale_Patch : FikaPatch
    {
        private static bool HasBeenSet = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Class1401).GetMethod(nameof(Class1401.ReloadBackendLocale));
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
