using Fika.Core.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Patches
{
    internal class Class1417_ReloadBackendLocale_Patch : FikaPatch
    {
        private static bool HasBeenSet = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Class1417).GetMethod(nameof(Class1417.ReloadBackendLocale));
        }

        [PatchPostfix]
        public static void Postfix(Task __result)
        {
            if (!HasBeenSet)
            {
                _ = Task.Run(() => FikaPlugin.Instance.WaitForLocales(__result));
                HasBeenSet = true;
            }
        }
    }
}
