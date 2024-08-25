// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic.Operations;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedInventoryController : Player.PlayerInventoryController
	{
		private IPlayerSearchController searchController;

		public override bool HasDiscardLimits => false;

		public override IPlayerSearchController PlayerSearchController
		{
			get
			{
				return searchController;
			}
		}

		public ObservedInventoryController(Player player, Profile profile, bool examined, MongoID currentId) : base(player, profile, examined)
		{
			mongoID_0 = currentId;
			searchController = new GClass1868();
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

		public override bool vmethod_0(GClass3087 operation)
		{
			return true;
		}

		public override SearchContentOperation vmethod_2(SearchableItemClass item)
		{
			return null;
		}

		public override void GetTraderServicesDataFromServer(string traderId)
		{
			// Do nothing
		}
		public void SetNewID(MongoID newId)
		{
			mongoID_0 = newId;
		}
	}
}
