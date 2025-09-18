using EFT.UI;
using SPT.Reflection.Patching;
using Fika.Core.UI.Custom;
using System.Reflection;

namespace Fika.Core.UI.Patches.MainMenuUI;

public class MenuScreen_Awake_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MenuScreen).GetMethod(nameof(MenuScreen.Awake));
    }

    [PatchPostfix]
    public static void Postfix(MenuScreen __instance)
    {
        __instance.gameObject.AddComponent<MainMenuUIScript>();
    }
}
