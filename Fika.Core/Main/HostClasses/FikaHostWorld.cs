using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.World;
using LiteNetLib;
using System.Collections.Generic;
using static Fika.Core.Networking.Packets.World.GenericSubPackets;

namespace Fika.Core.Main.HostClasses
{
    /// <summary>
    /// <see cref="World"/> used for the host to synchronize game logic
    /// </summary>
    public class FikaHostWorld : World
    {
        public List<LootSyncStruct> LootSyncPackets;
        public WorldPacket WorldPacket;

        private FikaServer _server;
        private GameWorld _gameWorld;

        public static FikaHostWorld Create(FikaHostGameWorld gameWorld)
        {
            FikaHostWorld hostWorld = gameWorld.gameObject.AddComponent<FikaHostWorld>();
            hostWorld._server = Singleton<FikaServer>.Instance;
            hostWorld._server.FikaHostWorld = hostWorld;
            hostWorld._gameWorld = gameWorld;
            hostWorld.LootSyncPackets = new List<LootSyncStruct>(8);
            hostWorld.WorldPacket = new()
            {
                ArtilleryPackets = new(8),
                SyncObjectPackets = new(8),
                GrenadePackets = new(8),
                LootSyncStructs = new(8),
                RagdollPackets = new(8)
            };
            return hostWorld;
        }

        protected void Update()
        {
            UpdateLootItems(_gameWorld.LootItems);
        }

        protected void LateUpdate()
        {
            int grenadesCount = _gameWorld.Grenades.Count;
            for (int i = 0; i < grenadesCount; i++)
            {
                Throwable throwable = _gameWorld.Grenades.GetByIndex(i);
                _gameWorld.method_2(throwable);
            }

            WorldPacket.GrenadePackets.AddRange(_gameWorld.GrenadesCriticalStates);
            WorldPacket.ArtilleryPackets.AddRange(_gameWorld.ArtilleryProjectilesStates);

            if (WorldPacket.HasData)
            {
                _server.SendReusableToAll(WorldPacket, DeliveryMethod.ReliableOrdered);
            }

            _gameWorld.GrenadesCriticalStates.Clear();
            _gameWorld.ArtilleryProjectilesStates.Clear();
        }

        public void UpdateLootItems(GClass816<int, LootItem> lootItems)
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
            GenericPacket packet = new()
            {
                NetId = player.iPlayer.Id,
                Type = SubPacket.EGenericSubPacketType.BorderZone,
                SubPacket = new BorderZoneEvent(player.iPlayer.ProfileId, zone.Id)
            };

            _server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }
    }
}
