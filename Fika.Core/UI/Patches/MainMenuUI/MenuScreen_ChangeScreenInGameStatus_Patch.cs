using System.Reflection;
using EFT.UI;
using Fika.Core.UI.Custom;
using SPT.Reflection.Patching;

namespace Fika.Core.UI.Patches.MainMenuUI;

public class MenuScreen_ChangeScreenInGameStatus_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MenuScreen)
            .GetMethod(nameof(MenuScreen.ChangeScreenInGameStatus));
    }

    [PatchPostfix]
    public static void Postfix(bool minimized)
    {
        if (!minimized && MainMenuUIScript.Exist)
        {
            MainMenuUIScript.Instance.UpdatePresence(FikaUIGlobals.EFikaPlayerPresence.IN_MENU);
        }
    }
}
