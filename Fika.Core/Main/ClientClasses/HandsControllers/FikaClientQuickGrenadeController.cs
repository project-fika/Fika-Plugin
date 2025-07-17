// © 2025 Lacyway All Rights Reserved

using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.FirearmController;
using UnityEngine;
using static Fika.Core.Networking.Packets.FirearmController.FirearmSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Main.ClientClasses.HandsControllers
{
    /// <summary>
    /// This is only used by AI
    /// </summary>
    public class FikaClientQuickGrenadeController : EFT.Player.QuickGrenadeThrowHandsController
    {
        protected FikaPlayer _fikaPlayer;

        public static FikaClientQuickGrenadeController Create(FikaPlayer player, ThrowWeapItemClass item)
        {
            FikaClientQuickGrenadeController controller = smethod_9<FikaClientQuickGrenadeController>(player, item);
            controller._fikaPlayer = player;
            return controller;
        }

        public override void vmethod_2(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Grenade,
                SubPacket = new GrenadePacket()
                {
                    HasGrenade = true,
                    GrenadeRotation = rotation,
                    GrenadePosition = position,
                    ThrowForce = force,
                    LowThrow = lowThrow
                }
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);

            base.vmethod_2(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
        }
    }
}
