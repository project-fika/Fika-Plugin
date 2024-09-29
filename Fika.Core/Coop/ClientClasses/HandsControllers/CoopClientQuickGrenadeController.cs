// © 2024 Lacyway All Rights Reserved

using Fika.Core.Coop.Players;
using Fika.Core.Networking.Packets;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
    /// <summary>
    /// This is only used by AI
    /// </summary>
    internal class CoopClientQuickGrenadeController : EFT.Player.QuickGrenadeThrowHandsController
	{
		protected CoopPlayer player;

		public static CoopClientQuickGrenadeController Create(CoopPlayer player, GrenadeClass item)
		{
			CoopClientQuickGrenadeController controller = smethod_9<CoopClientQuickGrenadeController>(player, item);
			controller.player = player;
			return controller;
		}

		public override void vmethod_2(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasGrenadePacket = true,
				GrenadePacket = new()
				{
					PacketType = SubPackets.GrenadePacketType.None,
					HasGrenade = true,
					GrenadeRotation = rotation,
					GrenadePosition = position,
					ThrowForce = force,
					LowThrow = lowThrow
				}
			});

			base.vmethod_2(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
		}
	}
}
