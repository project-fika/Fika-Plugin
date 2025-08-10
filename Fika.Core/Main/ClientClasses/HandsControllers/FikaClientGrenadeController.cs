// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.FirearmController;
using System;
using static Fika.Core.Networking.Packets.FirearmController.FirearmSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Main.ClientClasses.HandsControllers
{
    public class FikaClientGrenadeController : Player.GrenadeHandsController
    {
        protected FikaPlayer _fikaPlayer;
        private bool _isClient;

        public static FikaClientGrenadeController Create(FikaPlayer player, ThrowWeapItemClass item)
        {
            FikaClientGrenadeController controller = smethod_9<FikaClientGrenadeController>(player, item);
            controller._fikaPlayer = player;
            controller._isClient = FikaBackendUtils.IsClient;
            return controller;
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
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = GrenadePacket.FromValue(default, default, default, EGrenadePacketType.ExamineWeapon,
                false, false, false, false, false)
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            base.ExamineWeapon();
        }

        public override void HighThrow()
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = GrenadePacket.FromValue(default, default, default, EGrenadePacketType.HighThrow,
                false, false, false, false, false)
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            base.HighThrow();
        }

        public override void LowThrow()
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = GrenadePacket.FromValue(default, default, default, EGrenadePacketType.LowThrow,
                false, false, false, false, false)
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            base.LowThrow();
        }

        public override void PullRingForHighThrow()
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = GrenadePacket.FromValue(default, default, default, EGrenadePacketType.PullRingForHighThrow,
                false, false, false, false, false)
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            base.PullRingForHighThrow();
        }

        public override void PullRingForLowThrow()
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = GrenadePacket.FromValue(default, default, default, EGrenadePacketType.PullRingForLowThrow,
                false, false, false, false, false)
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            base.PullRingForLowThrow();
        }

        public override void vmethod_2(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = GrenadePacket.FromValue(rotation, position, force, EGrenadePacketType.None,
                true, lowThrow, false, false, false)
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            base.vmethod_2(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
        }

        public override void PlantTripwire()
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = GrenadePacket.FromValue(default, default, default, EGrenadePacketType.None,
                false, false, true, false, false)
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            base.PlantTripwire();
        }

        public override void ChangeFireMode(Weapon.EFireMode fireMode)
        {
            if (!CurrentOperation.CanChangeFireMode(fireMode))
            {
                return;
            }

            // Check for GClass increments
            Class1179 currentOperation = CurrentOperation;
            if (currentOperation != null)
            {
                if (currentOperation is not Class1184)
                {
                    if (currentOperation is TripwireStateManagerClass)
                    {
                        WeaponPacket packet = new()
                        {
                            NetId = _fikaPlayer.NetId,
                            Type = EFirearmSubPacketType.Grenade,
                            SubPacket = GrenadePacket.FromValue(default, default, default, EGrenadePacketType.None,
                false, false, false, true, false)
                        };
                        _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                    }
                }
                else
                {
                    WeaponPacket packet = new()
                    {
                        NetId = _fikaPlayer.NetId,
                        Type = EFirearmSubPacketType.Grenade,
                        SubPacket = GrenadePacket.FromValue(default, default, default, EGrenadePacketType.None,
                false, false, false, false, true)
                    };
                    _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                }
            }
            base.ChangeFireMode(fireMode);
        }

        public override void ActualDrop(Result<IHandsThrowController> controller, float animationSpeed, Action callback, bool fastDrop)
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.CancelGrenade
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            base.ActualDrop(controller, animationSpeed, callback, fastDrop);
        }
    }
}
