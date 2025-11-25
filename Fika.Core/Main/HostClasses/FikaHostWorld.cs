using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.World;
using System.Collections.Generic;

namespace Fika.Core.Main.HostClasses;

/// <summary>
/// <see cref="World"/> used for the host to synchronize game logic
/// </summary>
public class FikaHostWorld : World
{
    public List<LootSyncStruct> LootSyncPackets;
    public WorldPacket WorldPacket;

    private FikaServer _server;
    private GameWorld _gameWorld;
    private List<GrenadeDataPacketStruct> _grenadeData;
    private bool _hasCriticalData;
    private float _grenadeTimer;
    private float _grenadeInterval;

    public static FikaHostWorld Create(FikaHostGameWorld gameWorld)
    {
        var hostWorld = gameWorld.gameObject.AddComponent<FikaHostWorld>();
        hostWorld._server = Singleton<FikaServer>.Instance;
        hostWorld._server.FikaHostWorld = hostWorld;
        hostWorld._gameWorld = gameWorld;
        hostWorld.LootSyncPackets = new List<LootSyncStruct>(8);
        hostWorld.WorldPacket = new()
        {
            ArtilleryPackets = new(8),
            SyncObjectPackets = new(8),
            GrenadePackets = new(8),
            LootSyncStructs = new(8)
        };
        hostWorld._grenadeData = new(16);
        hostWorld._grenadeInterval = 1f;
        WindowBreaker.OnWindowHitAction += hostWorld.WindowBreaker_OnWindowHitAction;
        return hostWorld;
    }

    public override void OnDestroy()
    {
        WindowBreaker.OnWindowHitAction -= WindowBreaker_OnWindowHitAction;
        base.OnDestroy();
    }

    private void WindowBreaker_OnWindowHitAction(WindowBreaker windowBreaker, DamageInfoStruct damageInfo, WindowBreakingConfig.Crack crack, float angle)
    {
        _server.SendGenericPacket(EGenericSubPacketType.SyncableItem,
            SyncableItemPacket.FromValue(windowBreaker.NetId, damageInfo.HitPoint), true);
    }

    protected void Update()
    {
        UpdateLootItems(_gameWorld.LootItems);
    }

    /// <summary>
    /// Marks the current <see cref="WorldPacket"/> as critical
    /// </summary>
    internal void SetCritical()
    {
        _hasCriticalData = true;
    }

    protected void LateUpdate()
    {
        _grenadeTimer += Time.deltaTime;

        if (_grenadeTimer > _grenadeInterval)
        {
            var grenadesCount = _gameWorld.Grenades.Count;
            for (var i = 0; i < grenadesCount; i++)
            {
                var throwable = _gameWorld.Grenades.GetByIndex(i);
                if (throwable.HasNetData)
                {
                    _grenadeData.Add(throwable.GetNetPacket());
                }
            }

            _grenadeTimer -= _grenadeInterval;
        }

        if (_gameWorld.GrenadesCriticalStates.Count > 0)
        {
            _grenadeData.AddRange(_gameWorld.GrenadesCriticalStates);
            _gameWorld.GrenadesCriticalStates.Clear();
            SetCritical();
        }

        WorldPacket.GrenadePackets.AddRange(_grenadeData);
        WorldPacket.ArtilleryPackets.AddRange(_gameWorld.ArtilleryProjectilesStates);

        if (WorldPacket.HasData)
        {
            _server.SendReusableToAll(WorldPacket,
                _hasCriticalData ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable);

            _hasCriticalData = false;
        }

        _grenadeData.Clear();
        _gameWorld.ArtilleryProjectilesStates.Clear();
    }

    public void UpdateLootItems(GClass818<int, LootItem> lootItems)
    {
        for (var i = LootSyncPackets.Count - 1; i >= 0; i--)
        {
            var gstruct = LootSyncPackets[i];
            if (lootItems.TryGetByKey(gstruct.Id, out var lootItem))
            {
                if (lootItem is ObservedLootItem observedLootItem)
                {
                    observedLootItem.ApplyNetPacket(gstruct);
                }
                LootSyncPackets.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Sets up all the <see cref="BorderZone"/>s on the map
    /// </summary>
    public override void SubscribeToBorderZones(BorderZone[] zones)
    {
        foreach (var borderZone in zones)
        {
            borderZone.PlayerShotEvent += OnBorderZoneShot;
        }
    }

    /// <summary>
    /// Triggered when a <see cref="BorderZone"/> triggers (only runs on host)
    /// </summary>
    /// <param name="player"></param>
    /// <param name="zone"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    private void OnBorderZoneShot(IPlayerOwner player, BorderZone zone, float arg3, bool arg4)
    {
        _server.SendGenericPacket(EGenericSubPacketType.BorderZone,
            BorderZoneEvent.FromValue(player.iPlayer.ProfileId, zone.Id), true);
    }
}
