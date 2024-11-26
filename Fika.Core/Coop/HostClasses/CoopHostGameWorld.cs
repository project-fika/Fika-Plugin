using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using HarmonyLib;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.HostClasses
{
	/// <summary>
	/// <see cref="ClientLocalGameWorld"/> used in Fika for hosts to override methods and logic
	/// </summary>
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
			FikaHostWorld.Create(gameWorld);
			return gameWorld;
		}

		public override GrenadeFactoryClass CreateGrenadeFactory()
		{
			return new HostGrenadeFactory();
		}

		public override async Task InitLevel(ItemFactoryClass itemFactory, GClass2011 config, bool loadBundlesAndCreatePools = true, List<ResourceKey> resources = null, IProgress<LoadingProgressStruct> progress = null, CancellationToken ct = default)
		{
			await base.InitLevel(itemFactory, config, loadBundlesAndCreatePools, resources, progress, ct);
			MineManager.OnExplosion += OnMineExplode;
		}

		/// <summary>
		/// Triggers when a <see cref="MineDirectional"/> explodes
		/// </summary>
		/// <param name="directional"></param>
		private void OnMineExplode(MineDirectional directional)
		{
			if (!directional.gameObject.active)
			{
				return;
			}

			MinePacket packet = new()
			{
				MinePositon = directional.transform.position
			};
			Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}

		public override void Dispose()
		{
			base.Dispose();
			MineManager.OnExplosion -= OnMineExplode;
			NetManagerUtils.DestroyNetManager(true);
		}

		public override void InitAirdrop(string lootTemplateId = null, bool takeNearbyPoint = false, Vector3 position = default)
		{
			GameObject gameObject = method_19(takeNearbyPoint, position);
			if (gameObject == null)
			{
				FikaPlugin.Instance.FikaLogger.LogError("There are no airdrop points here!");
				return;
			}
			SynchronizableObject synchronizableObject = ClientSynchronizableObjectLogicProcessor.TakeFromPool(SynchronizableObjectType.AirPlane);
			if (synchronizableObject.Logic is AirplaneLogicClass airplaneLogicClass && airplaneLogicClass.offlineMode)
			{
				airplaneLogicClass.OfflineServerLogic.ContainerTemplateId = lootTemplateId;
			}
			ClientSynchronizableObjectLogicProcessor.InitSyncObject(synchronizableObject, gameObject.transform.position, Vector3.forward, -1);
		}

		public override GClass2400 SyncObjectProcessorFactory()
		{
			ClientSynchronizableObjectLogicProcessor = new SynchronizableObjectLogicProcessorClass
			{
				TripwireManager = new(Singleton<GameWorld>.Instance)
			};
			return ClientSynchronizableObjectLogicProcessor;
		}

		public override void PlantTripwire(Item item, string profileId, Vector3 fromPosition, Vector3 toPosition)
		{
			if (item is not ThrowWeapItemClass grenadeClass)
			{
				return;
			}

			if (SynchronizableObjectLogicProcessor.TripwireManager == null)
			{
				FikaPlugin.Instance.FikaLogger.LogError("TripwireManager was null! Creating new...");
				SynchronizableObjectLogicProcessor.TripwireManager = new GClass2402(this);
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
