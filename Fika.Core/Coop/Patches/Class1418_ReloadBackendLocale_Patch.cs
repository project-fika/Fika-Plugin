using Fika.Core.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Patches
{
    internal class Class1418_ReloadBackendLocale_Patch : FikaPatch
    {
        private static bool _hasBeenSet = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Class1418)
                .GetMethod(nameof(Class1418.ReloadBackendLocale));
        }

        [PatchPostfix]
        public static void Postfix(Task __result)
        {
            if (!_hasBeenSet)
            {
                _ = Task.Run(() => FikaPlugin.Instance.WaitForLocales(__result));
                _hasBeenSet = true;
            }
        }
    }
}
