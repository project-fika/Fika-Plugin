using EFT;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.HostClasses;
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
	}
}
