// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopBotHealthController(Profile.ProfileHealthClass healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth)
        : GControl4(healthInfo, player, inventoryController, skillManager, aiHealth)
    {
        private readonly CoopBot coopBot = (CoopBot)player;
        public override bool _sendNetworkSyncPackets
        {
            get
            {
                return true;
            }
        }

        public override void SendNetworkSyncPacket(NetworkHealthSyncPacketStruct packet)
        {
            if (packet.SyncType == NetworkHealthSyncPacketStruct.ESyncType.IsAlive && !packet.Data.IsAlive.IsAlive)
            {
                HealthSyncPacket deathPacket = coopBot.SetupCorpseSyncPacket(packet);
                coopBot.PacketSender.SendPacket(ref deathPacket);
                return;
            }

            if (packet.SyncType is NetworkHealthSyncPacketStruct.ESyncType.DestroyedBodyPart or NetworkHealthSyncPacketStruct.ESyncType.ApplyDamage or NetworkHealthSyncPacketStruct.ESyncType.BodyHealth)
            {
                HealthSyncPacket netPacket = new()
                {
                    NetId = coopBot.NetId,
                    Packet = packet
                };
                coopBot.PacketSender.SendPacket(ref netPacket);
            }
        }
    }
}
