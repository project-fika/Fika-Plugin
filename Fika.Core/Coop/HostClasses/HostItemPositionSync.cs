using EFT.Interactive;
using Fika.Core.Networking;
using LiteNetLib;
using UnityEngine;

namespace Fika.Core.Coop.HostClasses
{
	public class HostItemPositionSync : MonoBehaviour
	{
		private FikaServer server;
		private ObservedLootItem lootItem;
		private GStruct128 data;

		private Rigidbody Rigidbody
		{
			get
			{
				return lootItem.RigidBody;
			}
		}

		public static void Create(GameObject gameObject, FikaServer server, ObservedLootItem lootItem)
		{
			HostItemPositionSync posSync = gameObject.AddComponent<HostItemPositionSync>();
			posSync.server = server;
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
				server.SendDataToAll(ref endPacket, DeliveryMethod.ReliableOrdered);
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
			server.SendDataToAll(ref packet, DeliveryMethod.Unreliable);
		}
	}
}
