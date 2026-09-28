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
using KnifePacket = Fika.Core.Networking.Packets.FirearmController.SubPackets.KnifePacket;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

public class FikaClientKnifeController : Player.KnifeController
{
    protected FikaPlayer _fikaPlayer;

    public static FikaClientKnifeController Create(FikaPlayer player, KnifeComponent item)
    {
        var controller = CreateController<FikaClientKnifeController>(player, item);
        controller._fikaPlayer = player;
        return controller;
    }

    public override void CompassStateHandler(bool isActive)
    {
        //SendCompassState(CompassChangePacket.FromValue(isActive));
        base.CompassStateHandler(isActive);
    }

    public void SendCompassState(in CompassChangePacket packet)
    {
#if DEBUG
        FikaGlobals.LogInfo("Sending CompassPacket");
#endif
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override void ExamineWeapon()
    {
        base.ExamineWeapon();

        var packet = new KnifePacket(true, false, false, false);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override bool MakeKnifeKick()
    {
        var knifeKick = base.MakeKnifeKick();

        if (knifeKick)
        {
            var packet = new KnifePacket(false, true, false, false);
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }

        return knifeKick;
    }

    public override bool MakeAlternativeKick()
    {
        var alternateKnifeKick = base.MakeAlternativeKick();

        if (alternateKnifeKick)
        {
            var packet = new KnifePacket(false, false, true, false);
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }

        return alternateKnifeKick;
    }

    public override void BrakeCombo()
    {
        base.BrakeCombo();

        var packet = new KnifePacket(false, false, false, true);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override PlayerHitInfo ProcessHit(Player.KnifeRaycastHit hit, BallisticCollider ballisticCollider)
    {
        if (FikaBackendUtils.IsServer)
        {
            return base.ProcessHit(hit, ballisticCollider);
        }

        var shotInfo = base.ProcessHit(hit, ballisticCollider);
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
