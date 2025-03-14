using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using LiteNetLib;
using System.Collections.Generic;

namespace Fika.Core.Coop.ClientClasses
{
    /// <summary>
    /// <see cref="World"/> used for the client to synchronize game logic
    /// </summary>
    public class FikaClientWorld : World
    {
        public List<LootSyncStruct> LootSyncPackets;
        public List<AirplaneDataPacketStruct> SyncObjectPackets;
        public WorldPacket WorldPacket;

        private CoopClientGameWorld clientGameWorld;
        private FikaClient client;

        public static FikaClientWorld Create(CoopClientGameWorld gameWorld)
        {
            FikaClientWorld clientWorld = gameWorld.gameObject.AddComponent<FikaClientWorld>();
            clientWorld.clientGameWorld = gameWorld;
            clientWorld.LootSyncPackets = new(8);
            clientWorld.SyncObjectPackets = new(16);
            clientWorld.WorldPacket = new();
            clientWorld.client = Singleton<FikaClient>.Instance;
            clientWorld.client.FikaClientWorld = clientWorld;
            return clientWorld;
        }

        public void Update()
        {
            UpdateLootItems(clientGameWorld.LootItems);
            clientGameWorld.ClientSynchronizableObjectLogicProcessor.ProcessSyncObjectPackets(SyncObjectPackets);
        }

        protected void LateUpdate()
        {
            if (WorldPacket.HasData)
            {
                client.SendReusable(WorldPacket, DeliveryMethod.ReliableOrdered);
            }
        }

        public void UpdateLootItems(GClass797<int, LootItem> lootItems)
        {
            for (int i = LootSyncPackets.Count - 1; i >= 0; i--)
            {
                LootSyncStruct gstruct = LootSyncPackets[i];
                if (lootItems.TryGetByKey(gstruct.Id, out LootItem lootItem))
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
            foreach (BorderZone borderZone in zones)
            {
                borderZone.RemoveAuthority();
            }
        }
    }
}
