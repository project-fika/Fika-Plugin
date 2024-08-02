using EFT;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches.LocalGame
{
    public class GameWorld_Create_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.Create), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(typeof(ClientLocalGameWorld));
        }

        [PatchPrefix]
        public static bool Prefix(ref GameWorld __result, GameObject gameObject, PoolManager objectsFactory, EUpdateQueue updateQueue, string currentProfileId)
        {

            if (!FikaBackendUtils.RequestFikaWorld)
            {
                __result = CreateHideoutWorld(gameObject, objectsFactory, updateQueue, currentProfileId);
                return false;
            }

            if (FikaBackendUtils.IsServer)
            {
                __result = CoopHostGameWorld.Create(gameObject, objectsFactory, updateQueue, currentProfileId);
            }
            else
            {
                __result = CoopClientGameWorld.Create(gameObject, objectsFactory, updateQueue, currentProfileId);
            }
            return false;
        }

        private static GameWorld CreateHideoutWorld(GameObject gameObject, PoolManager objectsFactory, EUpdateQueue updateQueue, string currentProfileId)
        {
            HideoutGameWorld gameWorld = gameObject.AddComponent<HideoutGameWorld>();
            Traverse gameWorldTraverse = Traverse.Create(gameWorld);
            gameWorldTraverse.Field<PoolManager>("ObjectsFactory").Value = objectsFactory;
            gameWorldTraverse.Field<EUpdateQueue>("eupdateQueue_0").Value = updateQueue;
            gameWorld.SpeakerManager = gameObject.AddComponent<SpeakerManager>();
            gameWorld.ExfiltrationController = new ExfiltrationControllerClass();
            gameWorld.BufferZoneController = new BufferZoneControllerClass();
            gameWorld.CurrentProfileId = currentProfileId;
            gameWorld.UnityTickListener = GameWorldUnityTickListener.Create(gameObject, gameWorld);
            gameObject.AddComponent<FikaWorld>();
            return gameWorld;
        }
    }
}
