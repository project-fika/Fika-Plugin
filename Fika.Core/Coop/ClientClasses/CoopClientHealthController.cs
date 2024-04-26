// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.HealthSystem;
using Fika.Core.Coop.Players;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopClientHealthController(Profile.GClass1756 healthInfo, Player player, InventoryControllerClass inventoryController, SkillManager skillManager, bool aiHealth)
        : PlayerHealthController(healthInfo, player, inventoryController, skillManager, aiHealth)
    {
        private readonly CoopPlayer coopPlayer = (CoopPlayer)player;
        private readonly bool isBot = aiHealth;
        private readonly CoopBot coopBot = aiHealth ? (CoopBot)player : null;
        public override bool _sendNetworkSyncPackets
        {
            get
            {
                return true;
            }
        }

        public override void SendNetworkSyncPacket(GStruct346 packet)
        {
            if (packet.SyncType == GStruct346.ESyncType.IsAlive && !packet.Data.IsAlive.IsAlive)
            {
                if (!isBot)
                {
                    coopPlayer?.PacketSender?.HealthSyncPackets.Enqueue(coopPlayer.SetupDeathPacket(packet));
                }
                else
                {
                    coopBot?.PacketSender?.HealthSyncPackets.Enqueue(coopBot.SetupDeathPacket(packet));
                }
                return;
            }

            if (!isBot)
            {
                coopPlayer?.PacketSender?.HealthSyncPackets.Enqueue(new(coopPlayer.ProfileId)
                {
                    Packet = packet
                });
            }
            else
            {
                coopBot?.PacketSender?.HealthSyncPackets.Enqueue(new(coopBot.ProfileId)
                {
                    Packet = packet
                });
            }
        }
    }
}
