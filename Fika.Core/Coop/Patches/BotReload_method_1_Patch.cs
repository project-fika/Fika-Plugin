using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// This patch aims to alleviate problems with bots refilling mags too quickly causing a desync on <see cref="Item.Id"/>s by blocking firing for 0.5s
	/// </summary>
	public class BotReload_method_1_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BotReload).GetMethod(nameof(BotReload.method_1));
		}

		[PatchPrefix]
		public static bool Prefix(BotReload __instance, Player ____player, List<MagazineItemClass> ____preallocatedMagazineList, BotOwner ___botOwner_0)
		{
			___botOwner_0.ShootData.BlockFor(0.5f);
			BotReload.Class192 filter = new();
			if (____player == null || !____player.HealthController.IsAlive || __instance.ShootController == null)
			{
				return false;
			}
			Weapon item = __instance.ShootController.Item;
			if (item == null)
			{
				return false;
			}
			if (item.ReloadMode != Weapon.EReloadMode.ExternalMagazine)
			{
				return false;
			}
			filter.magazineSlot = item.GetMagazineSlot();
			if (filter.magazineSlot == null)
			{
				return false;
			}
			____preallocatedMagazineList.Clear();
			____player.InventoryController.GetReachableItemsOfTypeNonAlloc(____preallocatedMagazineList, filter.method_0);
			if (____preallocatedMagazineList.Count == 0)
			{
				return false;
			}
			int count = ____preallocatedMagazineList.Count;
			/*for (int i = 0; i < count; i++)
			{
				MagazineItemClass magazineItemClass = ____preallocatedMagazineList[i];
				if (magazineItemClass.Count > 1)
				{
#if DEBUG
					FikaPlugin.Instance.FikaLogger.LogWarning("Skipping magfill since there are mags with ammo!");
#endif
					return false;
				}
			}*/
			for (int i = 0; i < count; i++)
			{
				MagazineItemClass magazineItemClass = ____preallocatedMagazineList[i];
				if (magazineItemClass.Count < magazineItemClass.MaxCount)
				{
					__instance.method_2(item, magazineItemClass);
				}
			}
			____preallocatedMagazineList.Clear();
			return false;
		}
	}
}
