// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Networking.Packets;
using System;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
    internal class CoopClientGrenadeController : Player.GrenadeHandsController
	{
		protected CoopPlayer player;

		public static CoopClientGrenadeController Create(CoopPlayer player, GrenadeClass item)
		{
			CoopClientGrenadeController controller = smethod_9<CoopClientGrenadeController>(player, item);
			controller.player = player;
			return controller;
		}

		public override void ExamineWeapon()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = SubPackets.GrenadePacketType.ExamineWeapon
				}
			});
			base.ExamineWeapon();
		}

		public override void HighThrow()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = SubPackets.GrenadePacketType.HighThrow
				}
			});
			base.HighThrow();
		}

		public override void LowThrow()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = SubPackets.GrenadePacketType.LowThrow
				}
			});
			base.LowThrow();
		}

		public override void PullRingForHighThrow()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = SubPackets.GrenadePacketType.PullRingForHighThrow
				}
			});
			base.PullRingForHighThrow();
		}

		public override void PullRingForLowThrow()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = SubPackets.GrenadePacketType.PullRingForLowThrow
				}
			});
			base.PullRingForLowThrow();
		}

		public override void vmethod_2(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = SubPackets	.GrenadePacketType.None,
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
				HasGrenadePacket = true,
				GrenadePacket = new()
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

			Class1111 currentOperation = CurrentOperation;
			if (currentOperation != null)
			{
				if (currentOperation is not Class1116)
				{
					if (currentOperation is Class1117)
					{
						player.PacketSender.FirearmPackets.Enqueue(new()
						{
							HasGrenadePacket = true,
							GrenadePacket = new()
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
						HasGrenadePacket = true,
						GrenadePacket = new()
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
				CancelGrenade = true
			});
			base.ActualDrop(controller, animationSpeed, callback, fastDrop);
		}
	}
}
