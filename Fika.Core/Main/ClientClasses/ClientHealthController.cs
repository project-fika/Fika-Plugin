// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;

namespace Fika.Core.Main.ClientClasses;

public sealed class ClientHealthController(Profile.ProfileHealthClass healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth)
    : GClass3010(healthInfo, player, inventoryController, skillManager, aiHealth)
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
            _fikaPlayer.SetupCorpseSyncPacket(packet);
            return;
        }

        _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.HealthSync;
        _fikaPlayer.CommonPacket.SubPacket = HealthSyncPacket.FromValue(packet);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
    }
}
