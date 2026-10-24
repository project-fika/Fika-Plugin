using CommonAssets.Scripts.Game;
using System.Reflection;
using EFT;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.HostClasses;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.GameWorldPatches;

public class GameWorld_Create_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld)
            .GetMethod(nameof(GameWorld.Create), BindingFlags.Static | BindingFlags.Public)
            .MakeGenericMethod(typeof(ClientLocalGameWorld));
    }

    [PatchPrefix]
    public static bool Prefix(ref GameWorld __result, GameObject gameObject, ObjectsFactory objectsFactory, EUpdateQueue updateQueue, Il2CppSystem.Nullable<MongoID> currentProfileId)
    {
        if (FikaBackendUtils.RequestTutorialWorld)
        {
            FikaBackendUtils.RequestTutorialWorld = false;
            return true;
        }
        
        if (!FikaBackendUtils.RequestFikaWorld)
        {
            return true;
        }

        if (FikaBackendUtils.IsServer)
        {
            __result = FikaHostGameWorld.Create(gameObject, objectsFactory, updateQueue, currentProfileId);
        }
        else
        {
            __result = FikaClientGameWorld.Create(gameObject, objectsFactory, updateQueue, currentProfileId);
        }
        FikaBackendUtils.RequestFikaWorld = false;
        return false;
    }
}
