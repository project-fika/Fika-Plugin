using System.Net.Sockets;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

public class FikaClientPortableRangeFinderController : PortableRangeFinderController
{
    protected FikaPlayer _fikaPlayer;

    public static FikaClientPortableRangeFinderController Create(FikaPlayer player, Item item)
    {
        var controller = CreateController<FikaClientPortableRangeFinderController>(player, item);
        controller._fikaPlayer = player;
        return controller;
    }

    public override void CompassStateHandler(bool isActive)
    {
        base.CompassStateHandler(isActive);
        var packet = new UsableItemPacket(true, isActive, false, false, false);
        _fikaPlayer.PacketSender.NetworkManager.SendPlayerPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override bool ExamineWeapon()
    {
        var flag = base.ExamineWeapon();
        if (flag)
        {
            var packet = new UsableItemPacket(false, false, true, false, false);
            _fikaPlayer.PacketSender.NetworkManager.SendPlayerPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override void SetAim(bool value)
    {
        var isAiming = IsAiming;
        base.SetAim(value);

        if (IsAiming != isAiming)
        {
            var packet = new UsableItemPacket(false, false, false, true, isAiming);
            _fikaPlayer.PacketSender.NetworkManager.SendPlayerPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
    }
}
