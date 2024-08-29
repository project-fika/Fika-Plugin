// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic.Operations;
using Fika.Core.Coop.Players;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedInventoryController : Player.PlayerInventoryController, Interface15
	{
		private readonly IPlayerSearchController searchController;
		private readonly CoopPlayer coopPlayer;
		public override bool HasDiscardLimits => false;

		public override IPlayerSearchController PlayerSearchController
		{
			get
			{
				return searchController;
			}
		}

		public ObservedInventoryController(Player player, Profile profile, bool examined, MongoID firstId, ushort firstOperationId) : base(player, profile, examined)
		{
			mongoID_0 = firstId;
			ushort_0 = firstOperationId;
			searchController = new GClass1869();
			coopPlayer = (CoopPlayer)player;
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

		public override bool vmethod_0(GClass3088 operation)
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

		public void SetNewID(MongoID newId, ushort nextId)
		{
			mongoID_0 = newId;
			ushort_0 = nextId;
		}

		public GStruct416 CreateOperationFromDescriptor(GClass1642 descriptor)
		{
			method_13(descriptor);
			return descriptor.ToInventoryOperation(coopPlayer);
		}
	}
}
