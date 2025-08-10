// © 2025 Lacyway All Rights Reserved

using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.FirearmController;
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
        private WeaponPacket _packet;

        public static FikaClientQuickGrenadeController Create(FikaPlayer player, ThrowWeapItemClass item)
        {
            FikaClientQuickGrenadeController controller = smethod_9<FikaClientQuickGrenadeController>(player, item);
            controller._fikaPlayer = player;
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
    }
}
