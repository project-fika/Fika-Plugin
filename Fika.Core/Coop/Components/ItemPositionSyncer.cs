using Comfort.Common;
using EFT.Interactive;
using Fika.Core.Networking;
using LiteNetLib;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
	public class ItemPositionSyncer : MonoBehaviour
	{
		private FikaServer server;
		private FikaClient client;
		private bool isServer;
		private ObservedLootItem lootItem;
		private LootSyncStruct data;

		private Rigidbody Rigidbody
		{
			get
			{
				return lootItem.RigidBody;
			}
		}

		public static void Create(GameObject gameObject, bool isServer, ObservedLootItem lootItem)
		{
			ItemPositionSyncer posSync = gameObject.AddComponent<ItemPositionSyncer>();
			posSync.isServer = isServer;
			if (isServer)
			{
				posSync.server = Singleton<FikaServer>.Instance;
			}
			else
			{
				posSync.client = Singleton<FikaClient>.Instance;
			}
			posSync.lootItem = lootItem;
			posSync.data = new()
			{
				Id = lootItem.GetNetId()
			};
		}

		public void Start()
		{
			if (lootItem == null)
			{
				FikaPlugin.Instance.FikaLogger.LogError("HostItemPositionSync::Start: LootItem was null!");
				Destroy(this);
			}

			if (Rigidbody == null)
			{
				FikaPlugin.Instance.FikaLogger.LogError("HostItemPositionSync::Start: Rigidbody was null!");
				Destroy(this);
			}
		}

		public void FixedUpdate()
		{
			if (Rigidbody == null)
			{
				data.Position = lootItem.transform.position;
				data.Rotation = lootItem.transform.rotation;
				data.Velocity = Vector3.zero;
				data.AngularVelocity = Vector3.zero;
				data.Done = true;
				LootSyncPacket endPacket = new()
				{
					Data = data
				};
				if (isServer)
				{
					server.SendDataToAll(ref endPacket, DeliveryMethod.ReliableOrdered);
					Destroy(this);
					return;
				}

				client.SendData(ref endPacket, DeliveryMethod.ReliableOrdered);
				Destroy(this);
				return;
			}

			data.Position = lootItem.transform.position;
			data.Rotation = lootItem.transform.rotation;
			data.Velocity = Rigidbody.velocity;
			data.AngularVelocity = Rigidbody.angularVelocity;
			LootSyncPacket packet = new()
			{
				Data = data
			};
			if (isServer)
			{
				server.SendDataToAll(ref packet, DeliveryMethod.Unreliable);
				return;
			}

			client.SendData(ref packet, DeliveryMethod.Unreliable);
		}
	}
}
