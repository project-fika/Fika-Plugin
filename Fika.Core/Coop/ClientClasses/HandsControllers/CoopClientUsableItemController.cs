using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using static EFT.Player;

namespace Fika.Core.Coop.ClientClasses.HandsControllers
{
	public class CoopClientUsableItemController : UsableItemController
	{
		protected CoopPlayer player;

		public static CoopClientUsableItemController Create(CoopPlayer player, Item item)
		{
			CoopClientUsableItemController controller = smethod_6<CoopClientUsableItemController>(player, item);
			controller.player = player;
			return controller;
		}

		public override void CompassStateHandler(bool isActive)
		{
			base.CompassStateHandler(isActive);
			UsableItemPacket packet = new(player.NetId)
			{
				HasCompassState = true,
				CompassState = isActive
			};
			player.PacketSender.SendPacket(ref packet);
		}

		public override bool ExamineWeapon()
		{
			bool flag = base.ExamineWeapon();
			if (flag)
			{
				UsableItemPacket packet = new(player.NetId)
				{
					ExamineWeapon = true
				};
				player.PacketSender.SendPacket(ref packet);
			}
			return flag;
		}

		public override void SetAim(bool value)
		{
			bool isAiming = IsAiming;
			base.SetAim(value);
			if (IsAiming != isAiming)
			{
				UsableItemPacket packet = new(player.NetId)
				{
					HasAim = value,
					AimState = isAiming
				};
				player.PacketSender.SendPacket(ref packet);
			}
		}
	}
}
