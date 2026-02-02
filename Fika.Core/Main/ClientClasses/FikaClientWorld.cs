using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;

namespace Fika.Core.Main.ClientClasses;

/// <summary>
/// <see cref="World"/> used for the client to synchronize game logic
/// </summary>
public class FikaClientWorld : World
{
    public List<LootSyncStruct> LootSyncPackets;
    public List<AirplaneDataPacketStruct> SyncObjectPackets;
    public WorldPacket WorldPacket;

    private FikaClientGameWorld _clientGameWorld;
    private FikaClient _client;
    private bool _hasCriticalData;

    public static FikaClientWorld Create(FikaClientGameWorld gameWorld)
    {
        var clientWorld = gameWorld.gameObject.AddComponent<FikaClientWorld>();
        clientWorld._clientGameWorld = gameWorld;
        clientWorld.LootSyncPackets = new(8);
        clientWorld.SyncObjectPackets = new(16);
        clientWorld.WorldPacket = new()
        {
            ArtilleryPackets = new(8),
            SyncObjectPackets = new(8),
            GrenadePackets = new(8),
            LootSyncStructs = new(8)
        };
        clientWorld._client = Singleton<FikaClient>.Instance;
        clientWorld._client.FikaClientWorld = clientWorld;
        return clientWorld;
    }

    public void Update()
    {
        UpdateLootItems(_clientGameWorld.LootItems);
        _clientGameWorld.ClientSynchronizableObjectLogicProcessor.ProcessSyncObjectPackets(SyncObjectPackets);
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
        if (WorldPacket.HasData)
        {
            _client.SendReusable(WorldPacket,
                _hasCriticalData ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable);

            _hasCriticalData = false;
        }
    }

    public void UpdateLootItems(GClass818<int, LootItem> lootItems)
    {
        for (var i = LootSyncPackets.Count - 1; i >= 0; i--)
        {
            var lootSyncData = LootSyncPackets[i];
            if (lootItems.TryGetByKey(lootSyncData.Id, out var lootItem))
            {
                if (lootItem is ObservedLootItem observedLootItem)
                {
                    observedLootItem.ApplyNetPacket(lootSyncData);
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
        for (var i = 0; i < zones.Length; i++)
        {
            var borderZone = zones[i];
            borderZone.RemoveAuthority();
        }
    }
}
