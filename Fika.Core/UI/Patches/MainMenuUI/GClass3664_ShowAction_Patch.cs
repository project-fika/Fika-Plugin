using Fika.Core.Patching;
using Fika.Core.UI.Custom;
using System.Reflection;
using static EFT.UI.InventoryScreen;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches;

public class GClass3664_ShowAction_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GClass3664).GetMethod(nameof(GClass3664.ShowAction));
    }

    [PatchPostfix]
    public static void Postfix(GClass3664 __instance)
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
