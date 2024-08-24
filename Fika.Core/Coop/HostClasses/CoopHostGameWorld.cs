using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.HostClasses;
using Fika.Core.Networking;
using HarmonyLib;
using LiteNetLib;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
	public class CoopHostGameWorld : ClientLocalGameWorld
	{
		private FikaServer Server
		{
			get
			{
				return Singleton<FikaServer>.Instance;
			}
		}

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

		public override GClass712 CreateGrenadeFactory()
		{
			return new HostGrenadeFactory();
		}

		public override void Start()
		{
			base.Start();
			RegisterBorderZones();
		}

		public override void InitAirdrop(bool takeNearbyPoint = false, Vector3 position = default)
		{
			GameObject gameObject = method_18(takeNearbyPoint, position);
			if (gameObject == null)
			{
				FikaPlugin.Instance.FikaLogger.LogError("There is no airdrop points here!");
				return;
			}
			SynchronizableObject synchronizableObject = ClientSynchronizableObjectLogicProcessor.TakeFromPool(SynchronizableObjectType.AirPlane);
			ClientSynchronizableObjectLogicProcessor.InitSyncObject(synchronizableObject, gameObject.transform.position, Vector3.forward, -1);

			/*SpawnSyncObjectPacket packet = new(synchronizableObject.ObjectId)
			{
				ObjectType = SynchronizableObjectType.AirPlane,
				UniqueId = synchronizableObject.UniqueId,
				IsStatic = synchronizableObject.IsStatic,
				Position = gameObject.transform.position,
				Rotation = synchronizableObject.transform.rotation
			};

			Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);*/
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

			Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}

		public override void TriggerTripwire(TripwireSynchronizableObject tripwire)
		{
			SyncObjectPacket packet = new(tripwire.ObjectId)
			{
				ObjectType = SynchronizableObjectType.Tripwire,
				Data = new()
				{
					PacketData = new()
					{
						TripwireDataPacket = new()
						{
							State = ETripwireState.Active
						}
					},
					Position = tripwire.transform.position,
					Rotation = tripwire.transform.rotation.eulerAngles,
					IsActive = true
				}
			};

			Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
			base.TriggerTripwire(tripwire);			
		}

		public override void DeActivateTripwire(TripwireSynchronizableObject tripwire)
		{
			SyncObjectPacket packet = new(tripwire.ObjectId)
			{
				ObjectType = SynchronizableObjectType.Tripwire,
				Data = new()
				{
					PacketData = new()
					{
						TripwireDataPacket = new()
						{
							State = ETripwireState.Inert
						}
					},
					Position = tripwire.transform.position,
					Rotation = tripwire.transform.rotation.eulerAngles,
					IsActive = true
				}
			};

			Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
			base.DeActivateTripwire(tripwire);
			
		}
	}
}
