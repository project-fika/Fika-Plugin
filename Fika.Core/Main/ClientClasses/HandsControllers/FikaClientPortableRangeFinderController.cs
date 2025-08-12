using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

public class FikaClientPortableRangeFinderController : PortableRangeFinderController
{
    protected FikaPlayer _fikaPlayer;

    public static FikaClientPortableRangeFinderController Create(FikaPlayer player, Item item)
    {
        FikaClientPortableRangeFinderController controller = smethod_6<FikaClientPortableRangeFinderController>(player, item);
        controller._fikaPlayer = player;
        return controller;
    }

    public override void CompassStateHandler(bool isActive)
    {
        base.CompassStateHandler(isActive);
        _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.UsableItem;
        _fikaPlayer.CommonPacket.SubPacket = UsableItemPacket.FromValue(true, isActive, false, false, false);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
    }

    public override bool ExamineWeapon()
    {
        bool flag = base.ExamineWeapon();
        if (flag)
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.UsableItem;
            _fikaPlayer.CommonPacket.SubPacket = UsableItemPacket.FromValue(false, false, true, false, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override void SetAim(bool value)
    {
        bool isAiming = IsAiming;
        base.SetAim(value);

        if (IsAiming != isAiming)
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.UsableItem;
            _fikaPlayer.CommonPacket.SubPacket = UsableItemPacket.FromValue(false, false, false, true, isAiming);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }
    }
}
