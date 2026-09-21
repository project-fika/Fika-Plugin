using EFT.UI;
using System.Reflection;
using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.UI.Custom;
using SPTushonka.Reflection.Patching;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches.MainMenuUI;

/// <summary>
/// Sets the presence to in stash when the player opens the inventory
/// </summary>
public class InventoryScreen_ShowTab_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // The controller's ShowAction is virtual, and patching it makes the original call dispatch back into the patch
        return typeof(InventoryScreen).GetMethod(nameof(InventoryScreen.ShowTab));
    }

    [PatchPostfix]
    public static void Postfix(InventoryScreen __instance)
    {
        if (FikaGlobals.NetworkManager != null || InGameStatus.InRaid)
        {
            return;
        }

        if (MainMenuUIScript.Exist)
        {
            MainMenuUIScript.Instance.UpdatePresence(EFikaPlayerPresence.IN_STASH);
        }
    }
}
