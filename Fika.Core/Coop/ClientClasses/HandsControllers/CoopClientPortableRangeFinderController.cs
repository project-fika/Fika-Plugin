using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;

namespace Fika.Core.Coop.ClientClasses
{
	public class CoopClientPortableRangeFinderController : PortableRangeFinderController
	{
		protected CoopPlayer player;

		public static CoopClientPortableRangeFinderController Create(CoopPlayer player, Item item)
		{
			CoopClientPortableRangeFinderController controller = smethod_6<CoopClientPortableRangeFinderController>(player, item);
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
