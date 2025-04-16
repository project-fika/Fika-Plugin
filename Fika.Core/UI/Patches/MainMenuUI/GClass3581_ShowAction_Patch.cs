using Fika.Core.UI.Custom;
using Fika.Core.Patching;
using System.Reflection;
using static EFT.UI.InventoryScreen;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches
{
    public class GClass3581_ShowAction_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass3581).GetMethod(nameof(GClass3581.ShowAction));
        }

        [PatchPostfix]
        public static void Postfix(GClass3581 __instance)
        {
            if (!__instance.InRaid)
            {
                if (MainMenuUIScript.Exist)
                {
                    MainMenuUIScript.Instance.UpdatePresence(EFikaPlayerPresence.IN_STASH);
                }
            }
        }
    }
}
