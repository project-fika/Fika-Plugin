// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopClientHealthController(Profile.ProfileHealthClass healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth)
        : GClass2882(healthInfo, player, inventoryController, skillManager, aiHealth)
    {
        private readonly CoopPlayer _coopPlayer = (CoopPlayer)player;
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
                HealthSyncPacket deathPacket = _coopPlayer.SetupCorpseSyncPacket(packet);
                _coopPlayer.PacketSender.SendPacket(ref deathPacket);
                return;
            }

            HealthSyncPacket netPacket = new()
            {
                NetId = _coopPlayer.NetId,
                Packet = packet
            };
            _coopPlayer.PacketSender.SendPacket(ref netPacket);
        }
    }
}
