using Comfort.Common;
using EFT.Interactive;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using HarmonyLib;
using LiteNetLib;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
	internal class CorpsePositionSyncer : MonoBehaviour
	{
		private FieldInfo ragdollDoneField = AccessTools.Field(typeof(RagdollClass), "bool_2");

		private FikaServer server;
		private Corpse corpse;
		private GStruct129 data;

		public static void Create(GameObject gameObject, Corpse corpse)
		{
			CorpsePositionSyncer corpsePositionSyncer = gameObject.AddComponent<CorpsePositionSyncer>();
			corpsePositionSyncer.corpse = corpse;
			corpsePositionSyncer.server = Singleton<FikaServer>.Instance;
			corpsePositionSyncer.data = new()
			{
				Id = corpse.GetNetId()
			};
		}

		public void Start()
		{
			if (corpse == null)
			{
				FikaPlugin.Instance.FikaLogger.LogError("CorpsePositionSyncer::Start: Corpse was null!");
				Destroy(this);
			}

			if (!corpse.HasRagdoll)
			{
				FikaPlugin.Instance.FikaLogger.LogError("CorpsePositionSyncer::Start: Ragdoll was null!");
				Destroy(this);
			}
		}

		public void FixedUpdate()
		{
			if ((bool)ragdollDoneField.GetValue(corpse.Ragdoll))
			{
				data.Position = corpse.TrackableTransform.position;
				data.TransformSyncs = corpse.TransformSyncs;
				data.Done = true;
				CorpsePositionPacket endPacket = new()
				{
					Data = data
				};
				server.SendDataToAll(ref endPacket, DeliveryMethod.ReliableOrdered);
				Destroy(this);
				return;
			}

			data.Position = corpse.TrackableTransform.position;
			CorpsePositionPacket packet = new()
			{
				Data = data
			};

			server.SendDataToAll(ref packet, DeliveryMethod.Unreliable);
		}
	}
}
