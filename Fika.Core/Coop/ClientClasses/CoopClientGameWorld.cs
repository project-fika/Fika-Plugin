using EFT;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using Fika.Core.Coop.GameMode;
using HarmonyLib;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
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

		public override GClass711 CreateGrenadeFactory()
		{
			return new GClass712();
		}

		public override void Start()
		{
			base.Start();
			RegisterBorderZones();
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
