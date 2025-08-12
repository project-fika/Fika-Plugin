using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player;

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
        UsableItemPacket packet = new(_fikaPlayer.NetId)
        {
            HasCompassState = true,
            CompassState = isActive
        };
        _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered, true);
    }

    public override bool ExamineWeapon()
    {
        bool flag = base.ExamineWeapon();
        if (flag)
        {
            UsableItemPacket packet = new(_fikaPlayer.NetId)
            {
                ExamineWeapon = true
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override void SetAim(bool value)
    {
        bool isAiming = IsAiming;
        base.SetAim(value);
        if (IsAiming != isAiming)
        {
            UsableItemPacket packet = new(_fikaPlayer.NetId)
            {
                HasAim = value,
                AimState = isAiming
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered, true);
        }
    }
}
