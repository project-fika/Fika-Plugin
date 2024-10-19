// © 2024 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using Fika.Core.Coop.Players;

namespace Fika.Core.Coop.ObservedClasses
{
	internal class CoopObservedMedsController : EFT.Player.MedsController
	{
		private CoopPlayer coopPlayer;

		public static CoopObservedMedsController Create(CoopPlayer player, Item item, EBodyPart bodyPart, float amount, int animationVariant)
		{
			CoopObservedMedsController controller = smethod_6<CoopObservedMedsController>(player, item, bodyPart, amount, animationVariant);
			controller.coopPlayer = player;
			return controller;
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
