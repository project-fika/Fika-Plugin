using EFT;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Utils;
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
    }
}
