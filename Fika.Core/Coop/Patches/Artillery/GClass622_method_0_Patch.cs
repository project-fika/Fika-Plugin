using Comfort.Common;
using CommonAssets.Scripts.ArtilleryShelling;
using EFT;
using Fika.Core.Coop.GameMode;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Patches
{
    public class GClass622_method_0_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass622).GetMethod(nameof(GClass622.method_0));
        }

        [PatchPrefix]
        public static bool Prefix(GClass622 __instance, ref Task __result, CancellationToken cancellationToken)
        {
            __result = SetupServerArtilleryController(cancellationToken, __instance);
            return false;
        }

        private static async Task SetupServerArtilleryController(CancellationToken token, GClass622 instance)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            instance.gameWorld_0 = gameWorld;

            CoopGame coopGame = CoopGame.Instance;
            if (coopGame == null)
            {
                throw new NullReferenceException("GClass619_method_0_Patch: CoopGame was null!");
            }

            while (coopGame.Status != GameStatus.Started)
            {
                await Task.Yield();
            }

            BackendConfigSettingsClass.ArtilleryShellingGlobalSettings configuration = Singleton<BackendConfigSettingsClass>.Instance.ArtilleryShelling;
            instance.artilleryShellingGlobalSettings_0 = configuration;
            string locationId = gameWorld.LocationId;
            if (configuration.ArtilleryMapsConfigs.Keys.Contains(locationId))
            {
                instance.artilleryShellingMapConfiguration_0 = configuration.ArtilleryMapsConfigs[locationId];
            }

            instance.method_23(10);
            instance.method_5();
            instance.method_1();

            instance.bool_3 = true;
            coopGame.UpdateByUnity += instance.OnUpdate;

            ArtilleryShellingMapConfiguration mapConfiguration = instance.artilleryShellingMapConfiguration_0;
            if (mapConfiguration.PlanedShellingOn)
            {
                if (mapConfiguration.ShellingZones.Length != 0)
                {
                    int airDropTime = mapConfiguration.ArtilleryShellingAirDropSettings.AirDropTime;
                    instance.int_1 = airDropTime;
                    Vector3 airDropPosition = mapConfiguration.ArtilleryShellingAirDropSettings.AirDropPosition;
                    instance.vector3_0 = airDropPosition;
                    if (mapConfiguration.ArtilleryShellingAirDropSettings.UseAirDrop)
                    {
                        instance.InitAirdrop(airDropTime, airDropPosition).HandleExceptions();
                    }
                    await instance.method_8(mapConfiguration.InitShellingTimer);
                }
            }
        }
    }
}
