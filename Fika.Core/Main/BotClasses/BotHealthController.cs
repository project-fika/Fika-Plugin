// © 2026 Lacyway All Rights Reserved

using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;

namespace Fika.Core.Main.BotClasses;

public sealed class BotHealthController(Profile.HealthInfo healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth)
    : PlayerHealthController(healthInfo, player, inventoryController, skillManager, aiHealth)
{
    private readonly FikaBot _fikaBot = (FikaBot)player;
    public override bool _sendNetworkSyncPackets
    {
        get
        {
            return true;
        }
    }

    private bool ShouldSend(SyncHealthPacket.ESyncType syncType)
    {
        switch (syncType)
        {
            case SyncHealthPacket.ESyncType.AddEffect:
            case SyncHealthPacket.ESyncType.RemoveEffect:
            case SyncHealthPacket.ESyncType.IsAlive:
            case SyncHealthPacket.ESyncType.BodyHealth:
            case SyncHealthPacket.ESyncType.DestroyedBodyPart:
            case SyncHealthPacket.ESyncType.EffectStrength:
            case SyncHealthPacket.ESyncType.EffectNextState:
            case SyncHealthPacket.ESyncType.EffectMedResource:
            case SyncHealthPacket.ESyncType.EffectStimulatorBuff:
                return true;
            default:
                return false;
        }
    }

    public override void SendNetworkSyncPacket(SyncHealthPacket packet)
    {
        if (packet.SyncType == SyncHealthPacket.ESyncType.IsAlive && !packet.Data.IsAlive.IsAlive)
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


