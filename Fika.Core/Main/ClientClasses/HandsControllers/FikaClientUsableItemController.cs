using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player;
using static EFT.Player;

namespace Fika.Core.Main.ClientClasses.HandsControllers
{
    public class FikaClientUsableItemController : UsableItemController
    {
        protected FikaPlayer _coopPlayer;

        public static FikaClientUsableItemController Create(FikaPlayer player, Item item)
        {
            FikaClientUsableItemController controller = smethod_6<FikaClientUsableItemController>(player, item);
            controller._coopPlayer = player;
            return controller;
        }

        public override void CompassStateHandler(bool isActive)
        {
            base.CompassStateHandler(isActive);
            UsableItemPacket packet = new(_coopPlayer.NetId)
            {
                HasCompassState = true,
                CompassState = isActive
            };
            _coopPlayer.PacketSender.SendPacket(ref packet);
        }

        public override bool ExamineWeapon()
        {
            bool flag = base.ExamineWeapon();
            if (flag)
            {
                UsableItemPacket packet = new(_coopPlayer.NetId)
                {
                    ExamineWeapon = true
                };
                _coopPlayer.PacketSender.SendPacket(ref packet);
            }
            return flag;
        }

        public override void SetAim(bool value)
        {
            bool isAiming = IsAiming;
            base.SetAim(value);
            if (IsAiming != isAiming)
            {
                UsableItemPacket packet = new(_coopPlayer.NetId)
                {
                    HasAim = value,
                    AimState = isAiming
                };
                _coopPlayer.PacketSender.SendPacket(ref packet);
            }
        }
    }
}
