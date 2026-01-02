// © 2026 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;

namespace Fika.Core.Main.BotClasses;

public sealed class BotHealthController(Profile.ProfileHealthClass healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth)
    : GClass3010(healthInfo, player, inventoryController, skillManager, aiHealth)
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
            default:
                return false;
        }
    }

    public override void SendNetworkSyncPacket(NetworkHealthSyncPacketStruct packet)
    {
        if (packet.SyncType == NetworkHealthSyncPacketStruct.ESyncType.IsAlive && !packet.Data.IsAlive.IsAlive)
        {
            _fikaBot.SetupCorpseSyncPacket(packet);
            return;
        }

        if (ShouldSend(packet.SyncType))
        {
            _fikaBot.CommonPacket.Type = ECommonSubPacketType.HealthSync;
            _fikaBot.CommonPacket.SubPacket = HealthSyncPacket.FromValue(packet);
            _fikaBot.PacketSender.NetworkManager.SendNetReusable(ref _fikaBot.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }
    }
}


