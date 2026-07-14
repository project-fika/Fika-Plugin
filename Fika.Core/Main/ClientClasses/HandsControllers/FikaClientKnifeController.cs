// © 2026 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using EFT.NetworkPackets;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.FirearmController.SubPackets;
using Fika.Core.Networking.Packets.World;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

public class FikaClientKnifeController : Player.KnifeController
{
    protected FikaPlayer _fikaPlayer;
    private WeaponPacket _packet;

    public static FikaClientKnifeController Create(FikaPlayer player, KnifeComponent item)
    {
        var controller = smethod_9<FikaClientKnifeController>(player, item);
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
        var knifeKick = base.MakeKnifeKick();

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
        var alternateKnifeKick = base.MakeAlternativeKick();

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

    public override ShotInfoClass vmethod_0(Player.GStruct182 hit, BallisticCollider ballisticCollider)
    {
        if (FikaBackendUtils.IsServer)
        {
            return base.vmethod_0(hit, ballisticCollider);
        }

        var shotInfo = base.vmethod_0(hit, ballisticCollider);
        if (ballisticCollider == null || ballisticCollider.HitType == EHitType.Default)
        {
            return shotInfo;
        }

        var packet = new KnifeHitPacket
        {
            NetId = _fikaPlayer.NetId,
            HitType = ballisticCollider.HitType,
            HitId = ballisticCollider.NetId,
            HitPoint = hit.point
        };
        Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);

        return shotInfo;
    }
}
