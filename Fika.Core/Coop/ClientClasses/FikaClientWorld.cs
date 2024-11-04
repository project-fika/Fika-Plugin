using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using System.Collections.Generic;

namespace Fika.Core.Coop.ClientClasses
{
	/// <summary>
	/// <see cref="World"/> used for the client to synchronize game logic
	/// </summary>
	public class FikaClientWorld : World
	{
		public List<LootSyncStruct> LootSyncPackets;
		private CoopClientGameWorld clientGameWorld;

		public static FikaClientWorld Create(CoopClientGameWorld gameWorld)
		{
			FikaClientWorld clientWorld = gameWorld.gameObject.AddComponent<FikaClientWorld>();
			clientWorld.clientGameWorld = gameWorld;
			clientWorld.LootSyncPackets = new List<LootSyncStruct>(8);
			Singleton<FikaClient>.Instance.FikaClientWorld = clientWorld;
			return clientWorld;
		}

		public void Update()
		{
			UpdateLootItems(clientGameWorld.LootItems);
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
				borderZone.RemoveAuthority();
			}
		}
	}
}
