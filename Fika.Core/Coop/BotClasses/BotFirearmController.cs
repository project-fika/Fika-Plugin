using EFT.InventoryLogic;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Players;
using UnityEngine;

namespace Fika.Core.Coop.BotClasses
{
	public class BotFirearmController : CoopClientFirearmController
	{
		public override Vector3 WeaponDirection
		{
			get
			{
				return _player.LookDirection;
			}
		}

		public static BotFirearmController Create(CoopBot player, Weapon weapon)
		{
			BotFirearmController controller = smethod_6<BotFirearmController>(player, weapon);
			controller.player = player;
			return controller;
		}
	}
}
