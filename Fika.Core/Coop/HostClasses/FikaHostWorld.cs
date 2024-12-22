using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using LiteNetLib;
using System.Collections.Generic;

namespace Fika.Core.Coop.HostClasses
{
	/// <summary>
	/// <see cref="World"/> used for the host to synchronize game logic
	/// </summary>
	public class FikaHostWorld : World
	{
		public List<LootSyncStruct> LootSyncPackets;
		public WorldPacket WorldPacket;

		private FikaServer server;
		private GameWorld gameWorld;

		public static FikaHostWorld Create(CoopHostGameWorld gameWorld)
		{
			FikaHostWorld hostWorld = gameWorld.gameObject.AddComponent<FikaHostWorld>();
			hostWorld.server = Singleton<FikaServer>.Instance;
			hostWorld.server.FikaHostWorld = hostWorld;
			hostWorld.gameWorld = gameWorld;
			hostWorld.LootSyncPackets = new List<LootSyncStruct>(8);
			hostWorld.WorldPacket = new();
			return hostWorld;
		}

		protected void Update()
		{
			UpdateLootItems(gameWorld.LootItems);
		}

		protected void FixedUpdate()
		{
			int grenadesCount = gameWorld.Grenades.Count;
			for (int i = 0; i < grenadesCount; i++)
			{
				Throwable throwable = gameWorld.Grenades.GetByIndex(i);
				gameWorld.method_2(throwable);
			}
			
			WorldPacket.ThrowablePackets.AddRange(gameWorld.GrenadesCriticalStates);
			WorldPacket.ArtilleryPackets.AddRange(gameWorld.ArtilleryProjectilesStates);			

			if (WorldPacket.HasData)
			{
				server.SendReusableToAll(ref WorldPacket, DeliveryMethod.ReliableOrdered);
				WorldPacket.Flush();
			}

			gameWorld.GrenadesCriticalStates.Clear();
			gameWorld.ArtilleryProjectilesStates.Clear();
		}

		public void UpdateLootItems(GClass786<int, LootItem> lootItems)
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
			BorderZonePacket packet = new()
			{
				ProfileId = player.iPlayer.ProfileId,
				ZoneId = zone.Id
			};

			server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}
	}
}
