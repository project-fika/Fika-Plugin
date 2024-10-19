// © 2024 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using Fika.Core.Coop.Players;

namespace Fika.Core.Coop.ObservedClasses
{
	internal class CoopObservedKnifeController : EFT.Player.KnifeController
	{
		public static CoopObservedKnifeController Create(CoopPlayer player, KnifeComponent item)
		{
			CoopObservedKnifeController controller = smethod_9<CoopObservedKnifeController>(player, item);
			return controller;
		}
	}
}
