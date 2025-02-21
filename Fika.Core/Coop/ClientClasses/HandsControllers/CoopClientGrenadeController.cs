// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using System;
using UnityEngine;
using static Fika.Core.Networking.FirearmSubPackets;
using static Fika.Core.Networking.SubPacket;

namespace Fika.Core.Coop.ClientClasses
{
    internal class CoopClientGrenadeController : Player.GrenadeHandsController
    {
        protected CoopPlayer player;
        private bool isClient;

        public static CoopClientGrenadeController Create(CoopPlayer player, ThrowWeapItemClass item)
        {
            CoopClientGrenadeController controller = smethod_9<CoopClientGrenadeController>(player, item);
            controller.player = player;
            controller.isClient = FikaBackendUtils.IsClient;
            return controller;
        }

        public override bool CanThrow()
        {
            if (isClient)
            {
                return !player.WaitingForCallback && base.CanThrow();
            }

            return base.CanThrow();
        }

        public override void ExamineWeapon()
        {
            WeaponPacket packet = new()
            {
                NetId = player.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = new GrenadePacket()
                {
                    PacketType = EGrenadePacketType.ExamineWeapon
                }
            };
            player.PacketSender.SendPacket(ref packet);
            base.ExamineWeapon();
        }

        public override void HighThrow()
        {
            WeaponPacket packet = new()
            {
                NetId = player.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = new GrenadePacket()
                {
                    PacketType = EGrenadePacketType.HighThrow
                }
            };
            player.PacketSender.SendPacket(ref packet);
            base.HighThrow();
        }

        public override void LowThrow()
        {
            WeaponPacket packet = new()
            {
                NetId = player.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = new GrenadePacket()
                {
                    PacketType = EGrenadePacketType.LowThrow
                }
            };
            player.PacketSender.SendPacket(ref packet);
            base.LowThrow();
        }

        public override void PullRingForHighThrow()
        {
            WeaponPacket packet = new()
            {
                NetId = player.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = new GrenadePacket()
                {
                    PacketType = EGrenadePacketType.PullRingForHighThrow
                }
            };
            player.PacketSender.SendPacket(ref packet);
            base.PullRingForHighThrow();
        }

        public override void PullRingForLowThrow()
        {
            WeaponPacket packet = new()
            {
                NetId = player.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = new GrenadePacket()
                {
                    PacketType = EGrenadePacketType.PullRingForLowThrow
                }
            };
            player.PacketSender.SendPacket(ref packet);
            base.PullRingForLowThrow();
        }

        public override void vmethod_2(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
        {
            WeaponPacket packet = new()
            {
                NetId = player.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = new GrenadePacket()
                {
                    PacketType = EGrenadePacketType.None,
                    HasGrenade = true,
                    GrenadeRotation = rotation,
                    GrenadePosition = position,
                    ThrowForce = force,
                    LowThrow = lowThrow
                }
            };
            player.PacketSender.SendPacket(ref packet);
            base.vmethod_2(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
        }

        public override void PlantTripwire()
        {
            WeaponPacket packet = new()
            {
                NetId = player.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = new GrenadePacket()
                {
                    PlantTripwire = true
                }
            };
            player.PacketSender.SendPacket(ref packet);
            base.PlantTripwire();
        }

        public override void ChangeFireMode(Weapon.EFireMode fireMode)
        {
            if (!CurrentOperation.CanChangeFireMode(fireMode))
            {
                return;
            }

            // Check for GClass increments
            Class1154 currentOperation = CurrentOperation;
            if (currentOperation != null)
            {
                if (currentOperation is not Class1159)
                {
                    if (currentOperation is Class1160)
                    {
                        WeaponPacket packet = new()
                        {
                            NetId = player.NetId,
                            Type = EFirearmSubPacketType.Grenade,
                            SubPacket = new GrenadePacket()
                            {
                                ChangeToIdle = true
                            }
                        };
                        player.PacketSender.SendPacket(ref packet);
                    }
                }
                else
                {
                    WeaponPacket packet = new()
                    {
                        NetId = player.NetId,
                        Type = EFirearmSubPacketType.Grenade,
                        SubPacket = new GrenadePacket()
                        {
                            ChangeToPlant = true
                        }
                    };
                    player.PacketSender.SendPacket(ref packet);
                }
            }
            base.ChangeFireMode(fireMode);
        }

        public override void ActualDrop(Result<IHandsThrowController> controller, float animationSpeed, Action callback, bool fastDrop)
        {
            // TODO: Override Class1025

            WeaponPacket packet = new()
            {
                NetId = player.NetId,
                Type = EFirearmSubPacketType.CancelGrenade
            };
            player.PacketSender.SendPacket(ref packet);
            base.ActualDrop(controller, animationSpeed, callback, fastDrop);
        }
    }
}
