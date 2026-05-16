// © 2026 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;

namespace Fika.Core.Main.ClientClasses;

public sealed class ClientHealthController(Profile.ProfileHealthClass healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth) : GClass3010(healthInfo, player, inventoryController, skillManager, aiHealth)
{
    public bool ReviveEnabled { get; } = FikaPlugin.Instance.Settings.EnableReviveSystem.Value;
    public bool Downed { get; internal set; }

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
            if (ReviveEnabled)
            {
                if (Downed)
                {
                    return;
                }

                RestoreBodyPartNoEvents(EBodyPart.Head); // prevent blacked out head

                _fikaPlayer.ToggleDowned(true);
                return;
            }

            _fikaPlayer.SetupCorpseSyncPacket(packet);
            return;
        }

        if (packet.SyncType is NetworkHealthSyncPacketStruct.ESyncType.ApplyDamage)
        {
            return;
        }

        _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.HealthSync;
        _fikaPlayer.CommonPacket.SubPacket = HealthSyncPacket.FromValue(packet);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
    }

    private void RestoreBodyPartNoEvents(EBodyPart bodyPart)
    {
        var limb = Dictionary_0[bodyPart];
        if (limb.IsDestroyed)
        {
            limb.IsDestroyed = false;
            limb.Health.Current = 1f;

            method_44(bodyPart, EDamageType.Medicine);
            method_36(bodyPart);
        }
    }
}
