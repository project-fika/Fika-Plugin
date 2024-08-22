using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.HostClasses;
using Fika.Core.Networking;
using HarmonyLib;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
	public class CoopHostGameWorld : ClientLocalGameWorld
	{
		public static CoopHostGameWorld Create(GameObject gameObject, PoolManager objectsFactory, EUpdateQueue updateQueue, string currentProfileId)
		{
			CoopHostGameWorld gameWorld = gameObject.AddComponent<CoopHostGameWorld>();
			gameWorld.ObjectsFactory = objectsFactory;
			Traverse.Create(gameWorld).Field<EUpdateQueue>("eupdateQueue_0").Value = updateQueue;
			gameWorld.SpeakerManager = gameObject.AddComponent<SpeakerManager>();
			gameWorld.ExfiltrationController = new ExfiltrationControllerClass();
			gameWorld.BufferZoneController = new BufferZoneControllerClass();
			gameWorld.CurrentProfileId = currentProfileId;
			gameWorld.UnityTickListener = GameWorldUnityTickListener.Create(gameObject, gameWorld);
			gameWorld.AudioSourceCulling = gameObject.GetOrAddComponent<AudioSourceCulling>();
			gameObject.AddComponent<FikaHostWorld>();
			return gameWorld;
		}

		public override GClass711 CreateGrenadeFactory()
		{
			return new HostGrenadeFactory();
		}

		public override void Start()
		{
			base.Start();
			RegisterBorderZones();
		}

		public override void PlantTripwire(Item item, string profileId, Vector3 fromPosition, Vector3 toPosition)
		{
			if (item is not GrenadeClass grenadeClass)
			{
				return;
			}

			TripwireSynchronizableObject tripwireSynchronizableObject = (TripwireSynchronizableObject)SynchronizableObjectLogicProcessor.TakeFromPool(SynchronizableObjectType.Tripwire);
			tripwireSynchronizableObject.transform.SetPositionAndRotation(fromPosition, Quaternion.identity);
			SynchronizableObjectLogicProcessor.InitSyncObject(tripwireSynchronizableObject, fromPosition, Vector3.forward, -1);
			tripwireSynchronizableObject.SetupGrenade(grenadeClass, profileId, fromPosition, toPosition);
			SynchronizableObjectLogicProcessor.TripwireManager.AddTripwire(tripwireSynchronizableObject);
			Vector3 vector = (fromPosition + toPosition) * 0.5f;
			Singleton<BotEventHandler>.Instance.PlantTripwire(tripwireSynchronizableObject, vector);

			FikaServer server = Singleton<FikaServer>.Instance;
			SpawnSyncObjectPacket packet = new(tripwireSynchronizableObject.ObjectId)
			{
				ObjectType = SynchronizableObjectType.Tripwire,
				IsStatic = tripwireSynchronizableObject.IsStatic,
				GrenadeTemplate = grenadeClass.TemplateId,
				GrenadeId = grenadeClass.Id,
				ProfileId = profileId,
				Position = fromPosition,
				ToPosition = toPosition,
				Rotation = tripwireSynchronizableObject.transform.rotation
			};

			server.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
		}

		public override void TriggerTripwire(TripwireSynchronizableObject tripwire)
		{
			base.TriggerTripwire(tripwire);
			FikaServer server = Singleton<FikaServer>.Instance;
			SyncObjectPacket packet = new(tripwire.ObjectId)
			{
				ObjectType = SynchronizableObjectType.Tripwire,
				Triggered = true
			};
			server.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
		}

		public override void DeActivateTripwire(TripwireSynchronizableObject tripwire)
		{
			base.DeActivateTripwire(tripwire);
			FikaServer server = Singleton<FikaServer>.Instance;
			SyncObjectPacket packet = new(tripwire.ObjectId)
			{
				ObjectType = SynchronizableObjectType.Tripwire,
				Disarmed = true
			};
			server.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
		}
	}
}
