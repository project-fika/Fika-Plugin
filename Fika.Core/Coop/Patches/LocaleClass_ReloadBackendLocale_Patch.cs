using Fika.Core.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Patches
{
    internal class LocaleClass_ReloadBackendLocale_Patch : FikaPatch
    {
        private static bool _hasBeenSet = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocaleClass)
                .GetMethod(nameof(LocaleClass.ReloadBackendLocale));
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
