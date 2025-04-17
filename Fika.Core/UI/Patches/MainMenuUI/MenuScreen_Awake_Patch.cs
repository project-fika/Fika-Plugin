using EFT.UI;
using Fika.Core.UI.Custom;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
    public class MenuScreen_Awake_Patch : FikaPatch
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
}
