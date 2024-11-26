// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using System;
using UnityEngine;
using static Fika.Core.Networking.FirearmSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

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
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.Grenade,
				SubPacket = new GrenadePacket()
				{
					PacketType = EGrenadePacketType.ExamineWeapon
				}
			});
			base.ExamineWeapon();
		}

		public override void HighThrow()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.Grenade,
				SubPacket = new GrenadePacket()
				{
					PacketType = EGrenadePacketType.HighThrow
				}
			});
			base.HighThrow();
		}

		public override void LowThrow()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.Grenade,
				SubPacket = new GrenadePacket()
				{
					PacketType = EGrenadePacketType.LowThrow
				}
			});
			base.LowThrow();
		}

		public override void PullRingForHighThrow()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.Grenade,
				SubPacket = new GrenadePacket()
				{
					PacketType = EGrenadePacketType.PullRingForHighThrow
				}
			});
			base.PullRingForHighThrow();
		}

		public override void PullRingForLowThrow()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.Grenade,
				SubPacket = new GrenadePacket()
				{
					PacketType = EGrenadePacketType.PullRingForLowThrow
				}
			});
			base.PullRingForLowThrow();
		}

		public override void vmethod_2(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
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
			});
			base.vmethod_2(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
		}

		public override void PlantTripwire()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.Grenade,
				SubPacket = new GrenadePacket()
				{
					PlantTripwire = true
				}
			});
			base.PlantTripwire();
		}

		public override void ChangeFireMode(Weapon.EFireMode fireMode)
		{
			if (!CurrentOperation.CanChangeFireMode(fireMode))
			{
				return;
			}

			// Check for GClass increments
			Class1139 currentOperation = CurrentOperation;
			if (currentOperation != null)
			{
				if (currentOperation is not Class1144)
				{
					if (currentOperation is Class1145)
					{
						player.PacketSender.FirearmPackets.Enqueue(new()
						{
							Type = EFirearmSubPacketType.Grenade,
							SubPacket = new GrenadePacket()
							{
								ChangeToIdle = true
							}
						});
					}
				}
				else
				{
					player.PacketSender.FirearmPackets.Enqueue(new()
					{
						Type = EFirearmSubPacketType.Grenade,
						SubPacket = new GrenadePacket()
						{
							ChangeToPlant = true
						}
					});
				}
			}
			base.ChangeFireMode(fireMode);
		}

		public override void ActualDrop(Result<IHandsThrowController> controller, float animationSpeed, Action callback, bool fastDrop)
		{
			// TODO: Override Class1025

			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.CancelGrenade
			});
			base.ActualDrop(controller, animationSpeed, callback, fastDrop);
		}
	}
}
