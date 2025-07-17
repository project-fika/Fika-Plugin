// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player;

namespace Fika.Core.Main.ClientClasses
{
    public sealed class ClientHealthController(Profile.ProfileHealthClass healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth)
        : GClass2882(healthInfo, player, inventoryController, skillManager, aiHealth)
    {
        private readonly FikaPlayer _fikaPlayer = (FikaPlayer)player;
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
                HealthSyncPacket deathPacket = _fikaPlayer.SetupCorpseSyncPacket(packet);
                _fikaPlayer.PacketSender.SendPacket(ref deathPacket);
                return;
            }

            HealthSyncPacket netPacket = new()
            {
                NetId = _fikaPlayer.NetId,
                Packet = packet
            };
            _fikaPlayer.PacketSender.SendPacket(ref netPacket);
        }
    }
}
