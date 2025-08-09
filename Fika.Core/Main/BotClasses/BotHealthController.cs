// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player;
using LiteNetLib;

namespace Fika.Core.Main.BotClasses
{
    public sealed class BotHealthController(Profile.ProfileHealthClass healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth)
        : GClass2882(healthInfo, player, inventoryController, skillManager, aiHealth)
    {
        private readonly FikaBot _fikaBot = (FikaBot)player;
        public override bool _sendNetworkSyncPackets
        {
            get
            {
                return true;
            }
        }

        private bool ShouldSend(NetworkHealthSyncPacketStruct.ESyncType syncType)
        {
            switch (syncType)
            {
                case NetworkHealthSyncPacketStruct.ESyncType.AddEffect:
                case NetworkHealthSyncPacketStruct.ESyncType.RemoveEffect:
                case NetworkHealthSyncPacketStruct.ESyncType.IsAlive:
                case NetworkHealthSyncPacketStruct.ESyncType.BodyHealth:
                case NetworkHealthSyncPacketStruct.ESyncType.ApplyDamage:
                case NetworkHealthSyncPacketStruct.ESyncType.DestroyedBodyPart:
                case NetworkHealthSyncPacketStruct.ESyncType.EffectStrength:
                case NetworkHealthSyncPacketStruct.ESyncType.EffectNextState:
                case NetworkHealthSyncPacketStruct.ESyncType.EffectMedResource:
                case NetworkHealthSyncPacketStruct.ESyncType.EffectStimulatorBuff:
                    return true;
                case NetworkHealthSyncPacketStruct.ESyncType.EffectStateTime:
                case NetworkHealthSyncPacketStruct.ESyncType.Energy:
                case NetworkHealthSyncPacketStruct.ESyncType.Hydration:
                case NetworkHealthSyncPacketStruct.ESyncType.Temperature:
                case NetworkHealthSyncPacketStruct.ESyncType.DamageCoeff:
                case NetworkHealthSyncPacketStruct.ESyncType.HealthRates:
                case NetworkHealthSyncPacketStruct.ESyncType.HealerDone:
                case NetworkHealthSyncPacketStruct.ESyncType.BurnEyes:
                case NetworkHealthSyncPacketStruct.ESyncType.Poison:
                case NetworkHealthSyncPacketStruct.ESyncType.StaminaCoeff:
                default:
                    return false;
            }
        }

        public override void SendNetworkSyncPacket(NetworkHealthSyncPacketStruct packet)
        {
            if (packet.SyncType == NetworkHealthSyncPacketStruct.ESyncType.IsAlive && !packet.Data.IsAlive.IsAlive)
            {
                HealthSyncPacket deathPacket = _fikaBot.SetupCorpseSyncPacket(packet);
                _fikaBot.PacketSender.NetworkManager.SendData(ref deathPacket, DeliveryMethod.ReliableUnordered, true);
                return;
            }

            if (ShouldSend(packet.SyncType))
            {
                HealthSyncPacket netPacket = new()
                {
                    NetId = _fikaBot.NetId,
                    Packet = packet
                };
                _fikaBot.PacketSender.NetworkManager.SendData(ref netPacket, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }
}
