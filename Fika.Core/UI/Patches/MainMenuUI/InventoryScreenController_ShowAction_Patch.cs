using EFT.UI;
using System.Reflection;
using Comfort.Common;
using Fika.Core.Networking;
using Fika.Core.UI.Custom;
using SPT.Reflection.Patching;
using static EFT.UI.InventoryScreen;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches.MainMenuUI;

public class InventoryScreenController_ShowAction_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(InventoryScreenController)
            .GetMethod(nameof(InventoryScreenController.ShowAction));
    }

    [PatchPostfix]
    public static void Postfix(InventoryScreen.InventoryScreenController __instance)
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
