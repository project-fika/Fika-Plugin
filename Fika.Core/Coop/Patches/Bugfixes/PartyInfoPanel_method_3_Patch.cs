using EFT;
using EFT.InventoryLogic;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// Borrowed from Tyfon's UI Fixes with permission, a patch that fixes a bug if you inspect a player during loading when the controller is instantiated <br/><br/>
	/// Source code here: <see href="https://github.com/tyfon7/UIFixes/blob/main/src/Patches/FixPlayerInspectPatch.cs"/>
	/// </summary>
	public class PartyInfoPanel_method_3_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(PartyInfoPanel).GetMethod(nameof(PartyInfoPanel.method_3));
		}

		[PatchPrefix]
		public static bool Prefix(GClass1323 raidPlayer)
		{
			InventoryEquipment equipment = raidPlayer.PlayerVisualRepresentation.Equipment;
			if (equipment.CurrentAddress.GetOwnerOrNull() is Player.PlayerOwnerInventoryController)
			{
				return false;
			}

			return true;
		}
	}
}
