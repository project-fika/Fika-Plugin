using System.Reflection;
using Comfort.Common;
using Fika.Core.Networking;
using Fika.Core.UI.Custom;
using SPT.Reflection.Patching;
using static EFT.UI.InventoryScreen;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches.MainMenuUI;

public class GClass3871_ShowAction_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GClass3871)
            .GetMethod(nameof(GClass3871.ShowAction));
    }

    [PatchPostfix]
    public static void Postfix(GClass3871 __instance)
    {
        if (!__instance.InRaid && !Singleton<IFikaNetworkManager>.Instantiated)
        {
            if (MainMenuUIScript.Exist)
            {
                MainMenuUIScript.Instance.UpdatePresence(EFikaPlayerPresence.IN_STASH);
            }
        }
    }
}
