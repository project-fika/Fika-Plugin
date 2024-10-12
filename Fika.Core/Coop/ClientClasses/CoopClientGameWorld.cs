using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using Fika.Core.Coop.Utils;
using HarmonyLib;
using JetBrains.Annotations;
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
			FikaClientWorld.Create(gameWorld);
			return gameWorld;
		}

		public override LootItem CreateLootWithRigidbody(GameObject lootObject, Item item, string objectName, bool randomRotation, [CanBeNull] string[] validProfiles, out BoxCollider objectCollider, bool syncable, bool performPickUpValidation = true, float makeVisibleAfterDelay = 0)
		{
			if (syncable)
			{
				ObservedLootItem observedLootItem = ObservedLootItem.CreateLootWithRigidbody(lootObject, item, objectName, this, randomRotation, validProfiles, out objectCollider, performPickUpValidation, makeVisibleAfterDelay);
				Traverse.Create(observedLootItem).Field<bool>("bool_3").Value = true;
				return observedLootItem;
			}

			return base.CreateLootWithRigidbody(lootObject, item, objectName, randomRotation, validProfiles, out objectCollider, true, performPickUpValidation, makeVisibleAfterDelay);
		}

		public override GClass722 CreateGrenadeFactory()
		{
			return new GClass723();
		}

		public override void PlayerTick(float dt)
		{
			method_10(Class968.class968_0.method_5);
		}

		public override void vmethod_1(float dt)
		{
			// Do nothing
		}

		public override void InitAirdrop(string lootTemplateId = null, bool takeNearbyPoint = false, Vector3 position = default)
		{
			// Do nothing
		}

		public override GClass2329 SyncObjectProcessorFactory()
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
