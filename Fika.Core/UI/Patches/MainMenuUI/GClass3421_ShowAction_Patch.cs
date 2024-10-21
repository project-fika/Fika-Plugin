using Fika.Core.UI.Custom;
using SPT.Reflection.Patching;
using System.Reflection;
using static EFT.UI.InventoryScreen;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches
{
    public class GClass3421_ShowAction_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass3421).GetMethod(nameof(GClass3421.ShowAction));
        }

        [PatchPostfix]
        public static void Postfix(GClass3421 __instance)
        {
            if (!__instance.InRaid)
            {
                MainMenuUIScript.Instance.UpdatePresence(EFikaPlayerPresence.IN_STASH);
            }
        }
    }
}
