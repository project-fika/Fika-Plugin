// © 2026 Lacyway All Rights Reserved

using System;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.FirearmController.SubPackets;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

public class FikaClientGrenadeController : Player.GrenadeHandsController
{
    protected FikaPlayer _fikaPlayer;
    private bool _isClient;

    public static FikaClientGrenadeController Create(FikaPlayer player, ThrowWeap item)
    {
        var controller = CreateController<FikaClientGrenadeController>(player, item);
        controller._fikaPlayer = player;
        controller._isClient = FikaBackendUtils.IsClient;
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

    public override bool CanThrow()
    {
        if (_isClient)
        {
            return !_fikaPlayer.WaitingForCallback && base.CanThrow();
        }

        return base.CanThrow();
    }

    public override void ExamineWeapon()
    {
        var packet = new GrenadePacket(default, default, default,
            EGrenadePacketType.ExamineWeapon, false, false, false,
            false, false);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        base.ExamineWeapon();
    }

    public override void HighThrow()
    {
        var packet = new GrenadePacket(default, default, default,
            EGrenadePacketType.HighThrow, false, false, false,
            false, false);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        base.HighThrow();
    }

    public override void LowThrow()
    {
        var packet = new GrenadePacket(default, default, default,
            EGrenadePacketType.LowThrow, false, false, false,
            false, false);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        base.LowThrow();
    }

    public override void PullRingForHighThrow()
    {
        var packet = new GrenadePacket(default, default, default,
            EGrenadePacketType.PullRingForHighThrow, false, false,
            false, false, false);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        base.PullRingForHighThrow();
    }

    public override void PullRingForLowThrow()
    {
        var packet = new GrenadePacket(default, default, default,
            EGrenadePacketType.PullRingForLowThrow, false, false,
            false, false, false);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        base.PullRingForLowThrow();
    }

    public override void ThrowGrenade(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
    {
        var packet = new GrenadePacket(rotation, position, force,
            EGrenadePacketType.None, true, lowThrow, false, false,
            false);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        base.ThrowGrenade(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
    }

    public override void PlantTripwire()
    {
        var packet = new GrenadePacket(default, default, default,
            EGrenadePacketType.None, false, false, true, false,
            false);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        base.PlantTripwire();
    }

    public override void ChangeFireMode(Weapon.EFireMode fireMode)
    {
        if (!CurrentOperation.CanChangeFireMode(fireMode))
        {
            return;
        }

        // Check for GClass increments
        var currentOperation = CurrentOperation;
        if (currentOperation != null)
        {
            if (currentOperation is not Idling)
            {
                if (currentOperation is PlantTripwireOperation)
                {
                    var packet = new GrenadePacket(default, default, default,
                        EGrenadePacketType.None, false, false,
                        false, true, false);
                    _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
                }
            }
            else
            {
                var packet = new GrenadePacket(default, default, default,
                    EGrenadePacketType.None, false, false, false,
                    false, true);
                _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
            }
        }
        base.ChangeFireMode(fireMode);
    }

    public override void ActualDrop(Result<IGrenadeController> controller, float animationSpeed, Action callback, bool fastDrop)
    {
        var packet = new CancelGrenadePacket();
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        base.ActualDrop(controller, animationSpeed, callback, fastDrop);
    }
}
