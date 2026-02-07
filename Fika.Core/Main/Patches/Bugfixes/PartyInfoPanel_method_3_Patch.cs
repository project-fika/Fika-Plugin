using System.Reflection;
using EFT;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Bugfixes;

/// <summary>
/// Borrowed from Tyfon's UI Fixes with permission, a patch that fixes a bug if you inspect a player during loading when the controller is instantiated <br/><br/>
/// Source code here: <see href="https://github.com/tyfon7/UIFixes/blob/main/src/Patches/FixPlayerInspectPatch.cs"/>
/// </summary>
[IgnoreAutoPatch]
public class PartyInfoPanel_method_3_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PartyInfoPanel).GetMethod(nameof(PartyInfoPanel.method_3));
    }

    [PatchPrefix]
    public static bool Prefix(GroupPlayerViewModelClass raidPlayer)
    {
        var equipment = raidPlayer.PlayerVisualRepresentation.Equipment;
        if (equipment.CurrentAddress.GetOwnerOrNull() is Player.PlayerOwnerInventoryController)
        {
            return false;
        }

        return true;
    }
}
