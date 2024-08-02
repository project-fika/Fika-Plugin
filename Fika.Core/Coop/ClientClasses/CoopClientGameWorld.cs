using EFT;
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
            gameObject.AddComponent<FikaWorld>();
            return gameWorld;
        }

        public override GClass676 CreateGrenadeFactory()
        {
            return new GClass677();
        }

        public override void Start()
        {
            base.Start();
            RegisterBorderZones();
        }
    }
}
