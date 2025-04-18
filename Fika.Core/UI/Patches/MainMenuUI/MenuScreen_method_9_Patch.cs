using EFT.UI;
using Fika.Core.Patching;
using Fika.Core.UI.Custom;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
    public class MenuScreen_method_9_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod(nameof(MenuScreen.method_9));
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
}
