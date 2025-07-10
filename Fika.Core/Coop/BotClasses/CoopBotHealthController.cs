// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopBotHealthController(Profile.ProfileHealthClass healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth)
        : GClass2882(healthInfo, player, inventoryController, skillManager, aiHealth)
    {
        private readonly CoopBot _coopBot = (CoopBot)player;
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
                HealthSyncPacket deathPacket = _coopBot.SetupCorpseSyncPacket(packet);
                _coopBot.PacketSender.SendPacket(ref deathPacket);
                return;
            }

            if (ShouldSend(packet.SyncType))
            {
                HealthSyncPacket netPacket = new()
                {
                    NetId = _coopBot.NetId,
                    Packet = packet
                };
                _coopBot.PacketSender.SendPacket(ref netPacket);
            }
        }
    }
}
