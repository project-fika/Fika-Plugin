// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
	public class ObservedPacketSender : MonoBehaviour, IPacketSender
	{
		private CoopPlayer player;
		private bool isServer;
		public bool Enabled { get; set; } = true;
		public FikaServer Server { get; set; }
		public FikaClient Client { get; set; }
		public Queue<WeaponPacket> FirearmPackets { get; set; } = new(50);
		public Queue<DamagePacket> DamagePackets { get; set; } = new(50);
		public Queue<ArmorDamagePacket> ArmorDamagePackets { get; set; } = new(50);
		public Queue<InventoryPacket> InventoryPackets { get; set; } = new(50);
		public Queue<CommonPlayerPacket> CommonPlayerPackets { get; set; } = new(50);
		public Queue<HealthSyncPacket> HealthSyncPackets { get; set; } = new(50);

		protected void Awake()
		{
			player = GetComponent<ObservedCoopPlayer>();
			isServer = FikaBackendUtils.IsServer;
			if (isServer)
			{
				Server = Singleton<FikaServer>.Instance;
			}
			else
			{
				Client = Singleton<FikaClient>.Instance;
			}
		}

		public void Init()
		{

		}

		public void SendPacket<T>(ref T packet, bool force = false) where T : INetSerializable
		{
			if (!enabled)
			{
				return;
			}

			if (isServer)
			{
				Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
				return;
			}

			Client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
		}

		protected void Update()
		{
			if (player == null)
			{
				return;
			}

			if (DamagePackets.Count > 0)
			{
				if (isServer)
				{
					int damagePackets = DamagePackets.Count;
					for (int i = 0; i < damagePackets; i++)
					{
						DamagePacket damagePacket = DamagePackets.Dequeue();
						damagePacket.NetId = player.NetId;

						Server.SendDataToAll(ref damagePacket, DeliveryMethod.ReliableOrdered);
					}
					int armorDamagePackets = ArmorDamagePackets.Count;
					for (int i = 0; i < armorDamagePackets; i++)
					{
						ArmorDamagePacket armorDamagePacket = ArmorDamagePackets.Dequeue();
						armorDamagePacket.NetId = player.NetId;

						Server.SendDataToAll(ref armorDamagePacket, DeliveryMethod.ReliableOrdered);
					}
				}
				else
				{
					int damagePackets = DamagePackets.Count;
					for (int i = 0; i < damagePackets; i++)
					{
						DamagePacket damagePacket = DamagePackets.Dequeue();
						damagePacket.NetId = player.NetId;

						Client.SendData(ref damagePacket, DeliveryMethod.ReliableOrdered);
					}
					int armorDamagePackets = ArmorDamagePackets.Count;
					for (int i = 0; i < armorDamagePackets; i++)
					{
						ArmorDamagePacket armorDamagePacket = ArmorDamagePackets.Dequeue();
						armorDamagePacket.NetId = player.NetId;

						Client.SendData(ref armorDamagePacket, DeliveryMethod.ReliableOrdered);
					}
				}
			}
		}

		public void DestroyThis()
		{
			FirearmPackets.Clear();
			DamagePackets.Clear();
			InventoryPackets.Clear();
			CommonPlayerPackets.Clear();
			HealthSyncPackets.Clear();
			if (Server != null)
			{
				Server = null;
			}
			if (Client != null)
			{
				Client = null;
			}
			Destroy(this);
		}
	}
}
