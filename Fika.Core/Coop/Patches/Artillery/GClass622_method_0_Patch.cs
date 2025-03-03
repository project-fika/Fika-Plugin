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
            Traverse controllerTraverse = Traverse.Create(instance);
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            controllerTraverse.Field<GameWorld>("gameWorld_0").Value = gameWorld;

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
            controllerTraverse.Field<BackendConfigSettingsClass.ArtilleryShellingGlobalSettings>("artilleryShellingGlobalSettings_0").Value = configuration;
            string locationId = gameWorld.LocationId;
            if (configuration.ArtilleryMapsConfigs.Keys.Contains(locationId))
            {
                controllerTraverse.Field<ArtilleryShellingMapConfiguration>("artilleryShellingMapConfiguration_0").Value = configuration.ArtilleryMapsConfigs[locationId];
            }

            instance.method_23(10);
            instance.method_5();
            instance.method_1();

            controllerTraverse.Field<bool>("bool_3").Value = true;
            coopGame.UpdateByUnity += instance.OnUpdate;

            ArtilleryShellingMapConfiguration mapConfiguration = controllerTraverse.Field<ArtilleryShellingMapConfiguration>("artilleryShellingMapConfiguration_0").Value;
            if (mapConfiguration.PlanedShellingOn)
            {
                if (mapConfiguration.ShellingZones.Length != 0)
                {
                    int airDropTime = mapConfiguration.ArtilleryShellingAirDropSettings.AirDropTime;
                    controllerTraverse.Field<int>("int_1").Value = airDropTime;
                    Vector3 airDropPosition = mapConfiguration.ArtilleryShellingAirDropSettings.AirDropPosition;
                    controllerTraverse.Field<Vector3>("vector3_0").Value = airDropPosition;
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
