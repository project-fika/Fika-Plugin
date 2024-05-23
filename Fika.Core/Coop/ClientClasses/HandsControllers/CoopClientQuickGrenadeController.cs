// © 2024 Lacyway All Rights Reserved

using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
    /// <summary>
    /// This is only used by AI
    /// </summary>
    internal class CoopClientQuickGrenadeController : EFT.Player.QuickGrenadeThrowController
    {
        public CoopPlayer coopPlayer;

        private void Awake()
        {
            coopPlayer = GetComponent<CoopPlayer>();
        }

        public static CoopClientQuickGrenadeController Create(CoopPlayer player, GrenadeClass item)
        {
            return smethod_8<CoopClientQuickGrenadeController>(player, item);
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
    }
}
