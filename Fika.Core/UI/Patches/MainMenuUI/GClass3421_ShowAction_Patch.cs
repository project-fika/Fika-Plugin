using Fika.Core.UI.Custom;
using SPT.Reflection.Patching;
using System.Reflection;
using static EFT.UI.InventoryScreen;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches
{
    public class GClass3574_ShowAction_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass3574).GetMethod(nameof(GClass3574.ShowAction));
        }

        [PatchPostfix]
        public static void Postfix(GClass3574 __instance)
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
