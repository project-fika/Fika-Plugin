using Fika.Core.UI.Custom;
using SPT.Reflection.Patching;
using System.Reflection;
using static EFT.UI.InventoryScreen;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches
{
    public class GClass3577_ShowAction_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass3577).GetMethod(nameof(GClass3577.ShowAction));
        }

        [PatchPostfix]
        public static void Postfix(GClass3577 __instance)
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
