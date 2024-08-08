// © 2024 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using Fika.Core.Coop.Players;

namespace Fika.Core.Coop.ObservedClasses
{
	internal class CoopObservedKnifeController : EFT.Player.KnifeController
	{
		public CoopPlayer coopPlayer;

		private void Awake()
		{
			coopPlayer = GetComponent<CoopPlayer>();
		}

		public static CoopObservedKnifeController Create(CoopPlayer player, KnifeComponent item)
		{
			return smethod_8<CoopObservedKnifeController>(player, item);
		}
	}
}
