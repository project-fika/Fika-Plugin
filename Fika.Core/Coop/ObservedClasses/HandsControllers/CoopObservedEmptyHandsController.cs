// © 2024 Lacyway All Rights Reserved

using Fika.Core.Coop.Players;

namespace Fika.Core.Coop.ObservedClasses
{
	internal class CoopObservedEmptyHandsController : EFT.Player.EmptyHandsController
	{
		public CoopPlayer coopPlayer;

		private void Awake()
		{
			coopPlayer = GetComponent<CoopPlayer>();
		}

		public static CoopObservedEmptyHandsController Create(CoopPlayer player)
		{
			return smethod_5<CoopObservedEmptyHandsController>(player);
		}

		public override bool CanChangeCompassState(bool newState)
		{
			return false;
		}

		public override void OnCanUsePropChanged(bool canUse)
		{
			// Do nothing
		}

		public override void SetCompassState(bool active)
		{
			// Do nothing
		}
	}
}
