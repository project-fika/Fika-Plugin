// © 2024 Lacyway All Rights Reserved

using EFT;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedInventoryController : Player.PlayerInventoryController
	{
		public ObservedInventoryController(Player player, Profile profile, bool examined, MongoID currentId) : base(player, profile, examined)
		{
			mongoID_0 = currentId;
		}

		public override bool HasDiscardLimits => false;

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

		public void SetNewID(MongoID newId)
		{
			mongoID_0 = newId;
		}
	}
}
