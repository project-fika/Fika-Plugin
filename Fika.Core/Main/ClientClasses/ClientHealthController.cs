// © 2026 Lacyway All Rights Reserved

using System;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
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
            return !_bledOut && _maxRevives > 0 && _revives < _maxRevives;
        }
    }
    public float BleedoutTime { get; } = FikaPlugin.Instance.Settings.ReviveConfig.BleedoutTime;
    public bool ShouldBleedOut => BleedoutTime > 0f;

    private readonly int _maxRevives = FikaPlugin.Instance.Settings.ReviveConfig.MaxRevives;
    private readonly bool _headshotKills = FikaPlugin.Instance.Settings.ReviveConfig.HeadshotKills;
    private readonly bool _grenadesKills = FikaPlugin.Instance.Settings.ReviveConfig.GrenadesKills;
    private int _revives;
    private bool _bledOut;

    private readonly FikaPlayer _fikaPlayer = (FikaPlayer)player;

    public override bool _sendNetworkSyncPackets
    {
        get
        {
            return true;
        }
    }

    /// <summary>
    /// Checks whether last damage should kill from a grenade or headshot
    /// </summary>
    /// <returns><see langword="true"/> if the damage should kill; <see langword="false"/> if not</returns>
    public bool CheckIfDamageShouldInstantKill()
    {
        if (_grenadesKills && _fikaPlayer.LatestDamageInfo.DamageType is EDamageType.GrenadeFragment)
        {
            return true;
        }

        if (_fikaPlayer.LastDamagedBodyPart is EBodyPart.Head && _headshotKills)
        {
            return true;
        }

        return false;
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

        if (ShouldBleedOut)
        {
            var gameUi = MonoBehaviourSingleton<GameUI>.Instance;
            gameUi.BattleUiPanelExtraction.Close();
        }
    }

    public void BleedOut()
    {
        _bledOut = true;
        IsAlive = true; // need to be alive to trigger Kill() again
        Kill(_fikaPlayer.LatestDamageInfo.DamageType);
    }

    private bool TryProcessDownedState()
    {
        if (!ReviveEnabled)
        {
            return false;
        }

        if (_bledOut)
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

        if (_grenadesKills && _fikaPlayer.LatestDamageInfo.DamageType is EDamageType.GrenadeFragment)
        {
            return false;
        }

        if ((_fikaPlayer.LatestDamageInfo.DamageType & (EDamageType.Exhaustion | EDamageType.Dehydration)) != 0) // starving / dehydration will not trigger downed
        {
            return false;
        }

        if (_fikaPlayer.LastDamagedBodyPart is EBodyPart.Head && _headshotKills)
        {
            return false;
        }

        CurrentScreenSingletonClass.Instance.CloseAllScreensForced();

        RestoreBodyPartNoEvents(EBodyPart.Head); // prevent blacked out head
        RestoreBodyPartNoEvents(EBodyPart.Chest); // prevent blacked out chest

        RemoveAllBleedEffects();

        _fikaPlayer.ToggleDowned(true);
        return true;
    }

    /// <summary>
    /// Removes all bleed effects from the health controller
    /// </summary>
    /// <remarks>Mainly used to prevent instantly broken limbs after revive</remarks>
    private void RemoveAllBleedEffects()
    {
        for (var i = IReadOnlyList_0.Count - 1; i >= 0; i--)
        {
            if (IReadOnlyList_0[i] is HeavyBleeding heavyBleeding)
            {
                heavyBleeding.ForceRemove();
                continue;
            }

            if (IReadOnlyList_0[i] is LightBleeding lightBleeding)
            {
                lightBleeding.ForceRemove();
            }
        }
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
