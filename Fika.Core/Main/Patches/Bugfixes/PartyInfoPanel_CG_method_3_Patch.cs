using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.Matchmaker;
using SPTushonka.Reflection.Patching;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Patches.Bugfixes;

/// <summary>
/// Borrowed from Tyfon's UI Fixes with permission, a patch that fixes a bug if you inspect a player during loading when the controller is instantiated <br/><br/>
/// Source code here: <see href="https://github.com/tyfon7/UIFixes/blob/main/src/Patches/FixPlayerInspectPatch.cs"/>
/// </summary>
[IgnoreAutoPatch]
public class PartyInfoPanel_CG_method_3_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return Il2CppMethods.ByNativeName(typeof(PartyInfoPanel), "<Show>g__HandleOnPlayerEquipmentClick|12_2");
    }

    [PatchPrefix]
    public static bool Prefix(RaidPlayer raidPlayer)
    {
        var equipment = raidPlayer.PlayerVisualRepresentation.Equipment;
        if (equipment.CurrentAddress.GetOwnerOrNull() is Player.PlayerOwnerInventoryController)
        {
            return false;
        }

        return true;
    }
}
