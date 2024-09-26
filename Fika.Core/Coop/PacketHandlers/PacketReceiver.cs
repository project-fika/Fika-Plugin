// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
	public class PacketReceiver : MonoBehaviour
	{
		private CoopPlayer player;
		private ObservedCoopPlayer observedPlayer;
		public FikaServer Server { get; private set; }
		public FikaClient Client { get; private set; }
		public Queue<WeaponPacket> FirearmPackets { get; private set; } = new(50);
		public Queue<DamagePacket> DamagePackets { get; private set; } = new(50);
		public Queue<ArmorDamagePacket> ArmorDamagePackets { get; private set; } = new(50);
		public Queue<InventoryPacket> InventoryPackets { get; private set; } = new(50);
		public Queue<CommonPlayerPacket> CommonPlayerPackets { get; private set; } = new(50);
		public Queue<HealthSyncPacket> HealthSyncPackets { get; private set; } = new(50);

		protected void Awake()
		{
			player = GetComponent<CoopPlayer>();
			if (!player.IsYourPlayer)
			{
				observedPlayer = GetComponent<ObservedCoopPlayer>();
			}
		}

		protected void Start()
		{
			if (FikaBackendUtils.IsServer)
			{
				Server = Singleton<FikaServer>.Instance;
			}
			else
			{
				Client = Singleton<FikaClient>.Instance;
			}
		}

		protected void Update()
		{
			if (observedPlayer != null)
			{
				int healthSyncPackets = HealthSyncPackets.Count;
				if (healthSyncPackets > 0)
				{
					for (int i = 0; i < healthSyncPackets; i++)
					{
						HealthSyncPacket packet = HealthSyncPackets.Dequeue();
						if (packet.Packet.SyncType == GStruct358.ESyncType.IsAlive && !packet.Packet.Data.IsAlive.IsAlive)
						{
							observedPlayer.SetAggressor(packet.KillerId);
							observedPlayer.CorpseSyncPacket = packet.CorpseSyncPacket;
							if (packet.TriggerZones.Length > 0)
							{
								observedPlayer.TriggerZones.Clear();
								foreach (string triggerZone in packet.TriggerZones)
								{
									observedPlayer.TriggerZones.Add(triggerZone);
								}
							}
						}
						observedPlayer.NetworkHealthController.HandleSyncPacket(packet.Packet);
					}
				}
			}
			if (player == null)
			{
				return;
			}
			int firearmPackets = FirearmPackets.Count;
			if (firearmPackets > 0)
			{
				for (int i = 0; i < firearmPackets; i++)
				{
					player.HandleWeaponPacket(FirearmPackets.Dequeue());
				}
			}
			int damagePackets = DamagePackets.Count;
			if (damagePackets > 0)
			{
				for (int i = 0; i < damagePackets; i++)
				{
					DamagePacket damagePacket = DamagePackets.Dequeue();
					player.HandleDamagePacket(ref damagePacket);
				}
			}
			int armorDamagePackets = ArmorDamagePackets.Count;
			if (armorDamagePackets > 0)
			{
				for (int i = 0; i < armorDamagePackets; i++)
				{
					ArmorDamagePacket armorDamagePacket = ArmorDamagePackets.Dequeue();
					player.HandleArmorDamagePacket(ref armorDamagePacket);
				}
			}
			int inventoryPackets = InventoryPackets.Count;
			if (inventoryPackets > 0)
			{
				for (int i = 0; i < inventoryPackets; i++)
				{
					player.HandleInventoryPacket(InventoryPackets.Dequeue());
				}
			}
			int commonPlayerPackets = CommonPlayerPackets.Count;
			if (commonPlayerPackets > 0)
			{
				for (int i = 0; i < commonPlayerPackets; i++)
				{
					player.HandleCommonPacket(CommonPlayerPackets.Dequeue());
				}
			}
		}
	}
}
