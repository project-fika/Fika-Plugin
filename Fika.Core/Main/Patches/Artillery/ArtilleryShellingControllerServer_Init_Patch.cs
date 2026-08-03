using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using Fika.Core.Main.GameMode;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Artillery;

public class ArtilleryShellingControllerServer_Init_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ArtilleryShellingControllerServer)
            .GetMethod(nameof(ArtilleryShellingControllerServer.Init));
    }

    [PatchPrefix]
    public static bool Prefix(ArtilleryShellingControllerServer __instance, ref Task __result, CancellationToken cancellationToken)
    {
        __result = SetupServerArtilleryController(cancellationToken, __instance);
        return false;
    }

    private static async Task SetupServerArtilleryController(CancellationToken token, ArtilleryShellingControllerServer instance)
    {
        var gameWorld = Singleton<GameWorld>.Instance;
        instance._gameWorld = gameWorld;

        var fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame == null)
        {
            throw new NullReferenceException("ArtilleryShellingControllerServer_Init_Patch: CoopGame was null!");
        }

        while (fikaGame.GameController.GameInstance.Status != GameStatus.Started)
        {
            await Task.Yield();
        }

        var configuration = Singleton<GlobalConfiguration>.Instance.ArtilleryShelling;
        instance._artilleryShellingGlobalsSettings = configuration;
        var locationId = gameWorld.LocationId;
        if (configuration.ArtilleryMapsConfigs.Keys.Contains(locationId))
        {
            instance._currentMapConfiguration = configuration.ArtilleryMapsConfigs[locationId];
        }

        instance.InitProjectilesPool(10);
        instance.method_5();
        instance.method_1();

        instance._offlineMode = true;
        (fikaGame.GameController as HostGameController).UpdateByUnity += instance.OnUpdate;

        var mapConfiguration = instance._currentMapConfiguration;
        if (mapConfiguration.PlanedShellingOn)
        {
            if (mapConfiguration.ShellingZones.Length != 0)
            {
                var airDropTime = mapConfiguration.ArtilleryShellingAirDropSettings.AirDropTime;
                instance._initAirdropTime = airDropTime;
                var airDropPosition = mapConfiguration.ArtilleryShellingAirDropSettings.AirDropPosition;
                instance._airDropPosition = airDropPosition;
                if (mapConfiguration.ArtilleryShellingAirDropSettings.UseAirDrop)
                {
                    instance.InitAirdrop(airDropTime, airDropPosition).HandleExceptions();
                }
                await instance.StartPlanedShelling(mapConfiguration.InitShellingTimer);
            }
        }
    }
}
