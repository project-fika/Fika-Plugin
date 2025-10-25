// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.FirearmController.SubPackets;
using System;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

public class FikaClientGrenadeController : Player.GrenadeHandsController
{
    protected FikaPlayer _fikaPlayer;
    private bool _isClient;
    private WeaponPacket _packet;

    public static FikaClientGrenadeController Create(FikaPlayer player, ThrowWeapItemClass item)
    {
        FikaClientGrenadeController controller = smethod_9<FikaClientGrenadeController>(player, item);
        controller._fikaPlayer = player;
        controller._isClient = FikaBackendUtils.IsClient;
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
        _packet.Type = EFirearmSubPacketType.Grenade;
        _packet.SubPacket = GrenadePacket.FromValue(
            default,
            default,
            default,
            EGrenadePacketType.ExamineWeapon,
            false,
            false,
            false,
            false,
            false
        );
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        base.ExamineWeapon();
    }

    public override void HighThrow()
    {
        _packet.Type = EFirearmSubPacketType.Grenade;
        _packet.SubPacket = GrenadePacket.FromValue(
            default,
            default,
            default,
            EGrenadePacketType.HighThrow,
            false,
            false,
            false,
            false,
            false
        );
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        base.HighThrow();
    }

    public override void LowThrow()
    {
        _packet.Type = EFirearmSubPacketType.Grenade;
        _packet.SubPacket = GrenadePacket.FromValue(
            default,
            default,
            default,
            EGrenadePacketType.LowThrow,
            false,
            false,
            false,
            false,
            false
        );
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        base.LowThrow();
    }

    public override void PullRingForHighThrow()
    {
        _packet.Type = EFirearmSubPacketType.Grenade;
        _packet.SubPacket = GrenadePacket.FromValue(
            default,
            default,
            default,
            EGrenadePacketType.PullRingForHighThrow,
            false,
            false,
            false,
            false,
            false
        );
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        base.PullRingForHighThrow();
    }

    public override void PullRingForLowThrow()
    {
        _packet.Type = EFirearmSubPacketType.Grenade;
        _packet.SubPacket = GrenadePacket.FromValue(
            default,
            default,
            default,
            EGrenadePacketType.PullRingForLowThrow,
            false,
            false,
            false,
            false,
            false
        );
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        base.PullRingForLowThrow();
    }

    public override void vmethod_2(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
    {
        _packet.Type = EFirearmSubPacketType.Grenade;
        _packet.SubPacket = GrenadePacket.FromValue(
            rotation,
            position,
            force,
            EGrenadePacketType.None,
            true,
            lowThrow,
            false,
            false,
            false
        );
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        base.vmethod_2(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
    }

    public override void PlantTripwire()
    {
        _packet.Type = EFirearmSubPacketType.Grenade;
        _packet.SubPacket = GrenadePacket.FromValue(
            default,
            default,
            default,
            EGrenadePacketType.None,
            false,
            false,
            true,
            false,
            false
        );
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        base.PlantTripwire();
    }

    public override void ChangeFireMode(Weapon.EFireMode fireMode)
    {
        if (!CurrentOperation.CanChangeFireMode(fireMode))
        {
            return;
        }

        // Check for GClass increments
        Class1272 currentOperation = CurrentOperation;
        if (currentOperation != null)
        {
            if (currentOperation is not Class1277)
            {
                if (currentOperation is TripwireStateManagerClass)
                {
                    _packet.Type = EFirearmSubPacketType.Grenade;
                    _packet.SubPacket = GrenadePacket.FromValue(
                        default,
                        default,
                        default,
                        EGrenadePacketType.None,
                        false,
                        false,
                        false,
                        true,
                        false
                    );
                    _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
                }
            }
            else
            {
                _packet.Type = EFirearmSubPacketType.Grenade;
                _packet.SubPacket = GrenadePacket.FromValue(
                    default,
                    default,
                    default,
                    EGrenadePacketType.None,
                    false,
                    false,
                    false,
                    false,
                    true
                );
                _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
            }
        }
        base.ChangeFireMode(fireMode);
    }

    public override void ActualDrop(Result<IHandsThrowController> controller, float animationSpeed, Action callback, bool fastDrop)
    {
        _packet.Type = EFirearmSubPacketType.CancelGrenade;
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        base.ActualDrop(controller, animationSpeed, callback, fastDrop);
    }
}
