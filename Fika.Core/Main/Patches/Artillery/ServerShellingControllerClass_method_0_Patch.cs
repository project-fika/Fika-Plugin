using Comfort.Common;
using CommonAssets.Scripts.ArtilleryShelling;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Patching;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Fika.Core.Main.Patches;

public class ServerShellingControllerClass_method_0_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ServerShellingControllerClass).GetMethod(nameof(ServerShellingControllerClass.method_0));
    }

    [PatchPrefix]
    public static bool Prefix(ServerShellingControllerClass __instance, ref Task __result, CancellationToken cancellationToken)
    {
        __result = SetupServerArtilleryController(cancellationToken, __instance);
        return false;
    }

    private static async Task SetupServerArtilleryController(CancellationToken token, ServerShellingControllerClass instance)
    {
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
        instance.GameWorld_0 = gameWorld;

        IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame == null)
        {
            throw new NullReferenceException("ServerShellingControllerClass_method_0_Patch: CoopGame was null!");
        }

        while (fikaGame.GameController.GameInstance.Status != GameStatus.Started)
        {
            await Task.Yield();
        }

        BackendConfigSettingsClass.ArtilleryShellingGlobalSettings configuration = Singleton<BackendConfigSettingsClass>.Instance.ArtilleryShelling;
        instance.ArtilleryShellingGlobalSettings_0 = configuration;
        string locationId = gameWorld.LocationId;
        if (configuration.ArtilleryMapsConfigs.Keys.Contains(locationId))
        {
            instance.ArtilleryShellingMapConfiguration_0 = configuration.ArtilleryMapsConfigs[locationId];
        }

        instance.method_23(10);
        instance.method_5();
        instance.method_1();

        instance.Bool_3 = true;
        (fikaGame.GameController as HostGameController).UpdateByUnity += instance.OnUpdate;

        ArtilleryShellingMapConfiguration mapConfiguration = instance.ArtilleryShellingMapConfiguration_0;
        if (mapConfiguration.PlanedShellingOn)
        {
            if (mapConfiguration.ShellingZones.Length != 0)
            {
                int airDropTime = mapConfiguration.ArtilleryShellingAirDropSettings.AirDropTime;
                instance.Int_1 = airDropTime;
                Vector3 airDropPosition = mapConfiguration.ArtilleryShellingAirDropSettings.AirDropPosition;
                instance.Vector3_0 = airDropPosition;
                if (mapConfiguration.ArtilleryShellingAirDropSettings.UseAirDrop)
                {
                    instance.InitAirdrop(airDropTime, airDropPosition).HandleExceptions();
                }
                await instance.method_8(mapConfiguration.InitShellingTimer);
            }
        }
    }
}
