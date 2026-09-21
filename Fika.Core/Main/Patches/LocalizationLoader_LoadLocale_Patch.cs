using EFT;
using System.Reflection;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches;

internal class LocalizationLoader_LoadLocale_Patch : ModulePatch
{
    private static bool _hasBeenSet = false;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(LocalizationLoader)
            .GetMethod(nameof(LocalizationLoader.LoadLocale));
    }

    [PatchPostfix]
    public static void Postfix(Il2CppSystem.Threading.Tasks.Task __result)
    {
        if (!_hasBeenSet)
        {
            FikaPlugin.Instance.WaitForLocales(__result.AsManaged());
            _hasBeenSet = true;
        }
    }
}
