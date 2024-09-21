using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Utils;
using HarmonyLib;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
	/// <summary>
	/// <see cref="ClientLocalGameWorld"/> used in Fika for clients to override methods and logic
	/// </summary>
	public class CoopClientGameWorld : ClientLocalGameWorld
	{
		public static CoopClientGameWorld Create(GameObject gameObject, PoolManager objectsFactory, EUpdateQueue updateQueue, string currentProfileId)
		{
			CoopClientGameWorld gameWorld = gameObject.AddComponent<CoopClientGameWorld>();
			gameWorld.ObjectsFactory = objectsFactory;
			Traverse.Create(gameWorld).Field<EUpdateQueue>("eupdateQueue_0").Value = updateQueue;
			gameWorld.SpeakerManager = gameObject.AddComponent<SpeakerManager>();
			gameWorld.ExfiltrationController = new ExfiltrationControllerClass();
			gameWorld.BufferZoneController = new BufferZoneControllerClass();
			gameWorld.CurrentProfileId = currentProfileId;
			gameWorld.UnityTickListener = GameWorldUnityTickListener.Create(gameObject, gameWorld);
			gameWorld.AudioSourceCulling = gameObject.GetOrAddComponent<AudioSourceCulling>();
			gameObject.AddComponent<FikaWorld>();
			return gameWorld;
		}

		public override GClass712 CreateGrenadeFactory()
		{
			return new GClass713();
		}

		public override void PlayerTick(float dt)
		{
			method_10(Class951.class951_0.method_5);
		}

		public override void vmethod_1(float dt)
		{
			// Do nothing
		}

		public override void InitAirdrop(bool takeNearbyPoint = false, Vector3 position = default)
		{
			// Do nothing
		}

		public override GClass2300 SyncObjectProcessorFactory()
		{
			ClientSynchronizableObjectLogicProcessor = new SynchronizableObjectLogicProcessorClass
			{
				TripwireManager = new(Singleton<GameWorld>.Instance)
			};
			return ClientSynchronizableObjectLogicProcessor;
		}

		public override void Dispose()
		{
			base.Dispose();
			NetManagerUtils.DestroyNetManager(false);
			FikaBackendUtils.MatchingType = EMatchmakerType.Single;
		}

		public override void PlantTripwire(Item item, string profileId, Vector3 fromPosition, Vector3 toPosition)
		{
			// Do nothing
		}

		public override void TriggerTripwire(TripwireSynchronizableObject tripwire)
		{
			// Do nothing
		}

		public override void DeActivateTripwire(TripwireSynchronizableObject tripwire)
		{
			// Do nothing
		}
	}
}
