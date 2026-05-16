// © 2026 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;

namespace Fika.Core.Main.ClientClasses;

public sealed class ClientHealthController(Profile.ProfileHealthClass healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth) : GClass3010(healthInfo, player, inventoryController, skillManager, aiHealth)
{
    public bool ReviveEnabled { get; } = FikaPlugin.Instance.Settings.ReviveConfig.Enabled;
    public bool Downed { get; internal set; }
    public bool CanBeDowned
    {
        get
        {
            return _maxRevives > 0 && _revives < _maxRevives;
        }
    }

    private readonly int _maxRevives = FikaPlugin.Instance.Settings.ReviveConfig.MaxRevives;
    private readonly bool _headshotKills = FikaPlugin.Instance.Settings.ReviveConfig.HeadshotKills;
    private int _revives;

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
            if (!TryProcessDownedState())
            {
                _fikaPlayer.SetupCorpseSyncPacket(packet);
            }
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

    public void Revive()
    {
        _revives++;
        Downed = false;
    }

    private bool TryProcessDownedState()
    {
        if (!ReviveEnabled)
        {
            return false;
        }

        if (Downed)
        {
            return true;
        }

        if (!CanBeDowned)
        {
            return false;
        }

        if (_fikaPlayer.LastDamagedBodyPart is EBodyPart.Head)
        {
            if (_headshotKills)
            {
                return false;
            }

            RestoreBodyPartNoEvents(EBodyPart.Head); // prevent blacked out head
        }

        _fikaPlayer.ToggleDowned(true);
        return true;
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
