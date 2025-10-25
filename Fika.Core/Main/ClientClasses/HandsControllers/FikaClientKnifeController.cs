// © 2025 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.FirearmController.SubPackets;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

public class FikaClientKnifeController : EFT.Player.KnifeController
{
    protected FikaPlayer _fikaPlayer;
    private WeaponPacket _packet;

    public static FikaClientKnifeController Create(FikaPlayer player, KnifeComponent item)
    {
        FikaClientKnifeController controller = smethod_9<FikaClientKnifeController>(player, item);
        controller._fikaPlayer = player;
        controller._packet = new()
        {
            NetId = player.NetId
        };
        return controller;
    }

    public override void Destroy()
    {
        _packet = null;
        base.Destroy();
    }

    public override void CompassStateHandler(bool isActive)
    {
        //SendCompassState(CompassChangePacket.FromValue(isActive));
        base.CompassStateHandler(isActive);
    }

    public void SendCompassState(CompassChangePacket packet)
    {
#if DEBUG
        FikaGlobals.LogInfo("Sending CompassPacket");
#endif
        _packet.Type = EFirearmSubPacketType.CompassChange;
        _packet.SubPacket = packet;
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void ExamineWeapon()
    {
        base.ExamineWeapon();

        _packet.Type = EFirearmSubPacketType.Knife;
        _packet.SubPacket = KnifePacket.FromValue(true, false, false, false);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override bool MakeKnifeKick()
    {
        bool knifeKick = base.MakeKnifeKick();

        if (knifeKick)
        {
            _packet.Type = EFirearmSubPacketType.Knife;
            _packet.SubPacket = KnifePacket.FromValue(false, true, false, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        }

        return knifeKick;
    }

    public override bool MakeAlternativeKick()
    {
        bool alternateKnifeKick = base.MakeAlternativeKick();

        if (alternateKnifeKick)
        {
            _packet.Type = EFirearmSubPacketType.Knife;
            _packet.SubPacket = KnifePacket.FromValue(false, false, true, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        }

        return alternateKnifeKick;
    }

    public override void BrakeCombo()
    {
        base.BrakeCombo();

        _packet.Type = EFirearmSubPacketType.Knife;
        _packet.SubPacket = KnifePacket.FromValue(false, false, false, true);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }
}
