using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using LiteNetLib;
using System.Collections.Generic;

namespace Fika.Core.Main.ClientClasses
{
    /// <summary>
    /// <see cref="World"/> used for the client to synchronize game logic
    /// </summary>
    public class FikaClientWorld : World
    {
        public List<LootSyncStruct> LootSyncPackets;
        public List<AirplaneDataPacketStruct> SyncObjectPackets;
        public WorldPacket WorldPacket;

        private CoopClientGameWorld _clientGameWorld;
        private FikaClient _client;

        public static FikaClientWorld Create(CoopClientGameWorld gameWorld)
        {
            FikaClientWorld clientWorld = gameWorld.gameObject.AddComponent<FikaClientWorld>();
            clientWorld._clientGameWorld = gameWorld;
            clientWorld.LootSyncPackets = new(8);
            clientWorld.SyncObjectPackets = new(16);
            clientWorld.WorldPacket = new()
            {
                ArtilleryPackets = new(8),
                SyncObjectPackets = new(8),
                GrenadePackets = new(8),
                LootSyncStructs = new(8),
                RagdollPackets = new(8)
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

        protected void LateUpdate()
        {
            if (WorldPacket.HasData)
            {
                _client.SendReusable(WorldPacket, DeliveryMethod.ReliableOrdered);
            }
        }

        public void UpdateLootItems(GClass816<int, LootItem> lootItems)
        {
            for (int i = LootSyncPackets.Count - 1; i >= 0; i--)
            {
                LootSyncStruct lootSyncData = LootSyncPackets[i];
                if (lootItems.TryGetByKey(lootSyncData.Id, out LootItem lootItem))
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
            for (int i = 0; i < zones.Length; i++)
            {
                BorderZone borderZone = zones[i];
                borderZone.RemoveAuthority();
            }
        }
    }
}
