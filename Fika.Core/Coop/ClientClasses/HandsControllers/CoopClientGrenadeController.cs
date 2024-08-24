// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
	internal class CoopClientGrenadeController : EFT.Player.GrenadeHandsController
	{
		public CoopPlayer coopPlayer;

		private void Awake()
		{
			coopPlayer = GetComponent<CoopPlayer>();
		}

		public static CoopClientGrenadeController Create(CoopPlayer player, GrenadeClass item)
		{
			return smethod_9<CoopClientGrenadeController>(player, item);
		}

		public override void ExamineWeapon()
		{
			coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = FikaSerialization.GrenadePacket.GrenadePacketType.ExamineWeapon
				}
			});
			base.ExamineWeapon();
		}

		public override void HighThrow()
		{
			coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = FikaSerialization.GrenadePacket.GrenadePacketType.HighThrow
				}
			});
			base.HighThrow();
		}

		public override void LowThrow()
		{
			coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = FikaSerialization.GrenadePacket.GrenadePacketType.LowThrow
				}
			});
			base.LowThrow();
		}

		public override void PullRingForHighThrow()
		{
			coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = FikaSerialization.GrenadePacket.GrenadePacketType.PullRingForHighThrow
				}
			});
			base.PullRingForHighThrow();
		}

		public override void PullRingForLowThrow()
		{
			coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = FikaSerialization.GrenadePacket.GrenadePacketType.PullRingForLowThrow
				}
			});
			base.PullRingForLowThrow();
		}

		public override void vmethod_2(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
		{
			coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = FikaSerialization.GrenadePacket.GrenadePacketType.None,
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
			coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
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

			Class1093 currentOperation = CurrentOperation;
			if (currentOperation != null)
			{
				if (currentOperation is not Class1097)
				{
					if (currentOperation is Class1098)
					{
						coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
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
					coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
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

			coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
			{
				CancelGrenade = true
			});
			base.ActualDrop(controller, animationSpeed, callback, fastDrop);
		}
	}
}
