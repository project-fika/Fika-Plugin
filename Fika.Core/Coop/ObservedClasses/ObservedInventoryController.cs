// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic.Operations;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedInventoryController : Player.PlayerInventoryController
	{

		public override bool HasDiscardLimits => false;

		public override IPlayerSearchController PlayerSearchController
		{
			get
			{
				return null;
			}
		}

		public ObservedInventoryController(Player player, Profile profile, bool examined) : base(player, profile, examined)
		{

		}

		public override void StrictCheckMagazine(MagazineClass magazine, bool status, int skill = 0, bool notify = false, bool useOperation = true)
		{
			// Do nothing
		}

		public override void OnAmmoLoadedCall(int count)
		{
			// Do nothing
		}

		public override void OnAmmoUnloadedCall(int count)
		{
			// Do nothing
		}

		public override void OnMagazineCheckCall()
		{
			// Do nothing
		}

		public override bool IsInventoryBlocked()
		{
			return false;
		}

		public override SearchContentOperation vmethod_2(SearchableItemClass item)
		{
			return null;
		}

		/*public override void InProcess(TraderControllerClass executor, Item item, ItemAddress to, bool succeed, GInterface338 operation, Callback callback)
        {
            if (!succeed)
            {
                callback.Succeed();
                return;
            }
            if (!executor.CheckTransferOwners(item, to, out Error error))
            {
                callback.Fail(error.ToString());
                return;
            }
            callback.Succeed();
        }*/
	}
}
