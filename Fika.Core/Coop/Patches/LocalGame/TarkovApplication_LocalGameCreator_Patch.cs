using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using HarmonyLib;
using JsonType;
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Patches.LocalGame
{
    /// <summary>
    /// Created by: Paulov
    /// Paulov: Overwrite and use our own CoopGame instance instead
    /// </summary>
    internal class TarkovApplication_LocalGameCreator_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_47));

        [PatchPrefix]
        public static bool Prefix(ref Task __result, TarkovApplication __instance, TimeAndWeatherSettings timeAndWeather, MatchmakerTimeHasCome.TimeHasComeScreenClass timeHasComeScreenController,
            RaidSettings ____raidSettings, InputTree ____inputTree, GameDateTime ____localGameDateTime, float ____fixedDeltaTime, MetricsEventsClass metricsEvents, MetricsConfigClass metricsConfig)
        {
#if DEBUG
            Logger.LogInfo("TarkovApplication_LocalGameCreator_Patch:Prefix");

#endif
            __result = CreateFikaGame(__instance, timeAndWeather, timeHasComeScreenController, ____raidSettings,
                ____inputTree, ____localGameDateTime, ____fixedDeltaTime, metricsEvents, metricsConfig);
            return false;
        }

        public static async Task CreateFikaGame(TarkovApplication instance, TimeAndWeatherSettings timeAndWeather, MatchmakerTimeHasCome.TimeHasComeScreenClass timeHasComeScreenController,
            RaidSettings raidSettings, InputTree inputTree, GameDateTime localGameDateTime, float fixedDeltaTime, MetricsEventsClass metricsEvents, MetricsConfigClass metricsConfig)
        {
            bool isServer = FikaBackendUtils.IsServer;

            metricsEvents.SetGamePrepared();

            LocationSettingsClass.Location location = raidSettings.SelectedLocation;

            FikaBackendUtils.ScreenController = timeHasComeScreenController;

            if (Singleton<NotificationManagerClass>.Instantiated)
            {
                Singleton<NotificationManagerClass>.Instance.Deactivate();
            }

            ISession session = instance.Session;

            Profile profile = session.GetProfileBySide(raidSettings.Side);

            bool isDedicatedHost = session.Profile.Nickname.StartsWith("dedicated_");
            if (isDedicatedHost)
            {
                FikaBackendUtils.IsDedicated = true;
            }

            profile.Inventory.Stash = null;
            profile.Inventory.QuestStashItems = null;
            profile.Inventory.DiscardLimits = Singleton<ItemFactory>.Instance.GetDiscardLimits();

#if DEBUG
            Logger.LogInfo("TarkovApplication_LocalGameCreator_Patch:Postfix: Attempt to set Raid Settings");
#endif

            await session.SendRaidSettings(raidSettings);
            LocalRaidSettings localRaidSettings = new()
            {
                location = raidSettings.LocationId,
                timeVariant = raidSettings.SelectedDateTime,
                mode = ELocalMode.PVE_OFFLINE,
                playerSide = raidSettings.Side
            };
            Traverse applicationTraverse = Traverse.Create(instance);
            applicationTraverse.Field<LocalRaidSettings>("localRaidSettings_0").Value = localRaidSettings;

            LocalSettings localSettings = await instance.Session.LocalRaidStarted(localRaidSettings);
            applicationTraverse.Field<LocalRaidSettings>("localRaidSettings_0").Value.serverId = localSettings.serverId;
            applicationTraverse.Field<LocalRaidSettings>("localRaidSettings_0").Value.selectedLocation = localSettings.locationLoot;

            GClass1222 profileInsurance = localSettings.profileInsurance;
            if ((profileInsurance?.insuredItems) != null)
            {
                profile.InsuredItems = localSettings.profileInsurance.insuredItems;
            }

            if (!isServer)
            {
                timeHasComeScreenController.ChangeStatus("Joining coop game...");

                RaidSettingsRequest data = new();
                RaidSettingsResponse raidSettingsResponse = await FikaRequestHandler.GetRaidSettings(data);

                raidSettings.MetabolismDisabled = raidSettingsResponse.MetabolismDisabled;
                raidSettings.PlayersSpawnPlace = (EPlayersSpawnPlace)Enum.Parse(typeof(EPlayersSpawnPlace), raidSettingsResponse.PlayersSpawnPlace);
            }
            else
            {
                timeHasComeScreenController.ChangeStatus("Creating coop game...");
            }

            StartHandler startHandler = new(instance, session.Profile, session.ProfileOfPet, raidSettings.SelectedLocation, timeHasComeScreenController);

            TimeSpan raidLimits = instance.method_48(raidSettings.SelectedLocation.EscapeTimeLimit);

            CoopGame coopGame = CoopGame.Create(inputTree, profile, localGameDateTime, instance.Session.InsuranceCompany,
                MonoBehaviourSingleton<MenuUI>.Instance, MonoBehaviourSingleton<GameUI>.Instance, location,
                timeAndWeather, raidSettings.WavesSettings, raidSettings.SelectedDateTime, startHandler.HandleStop,
                fixedDeltaTime, instance.PlayerUpdateQueue, instance.Session, raidLimits, metricsEvents,
                new GClass2182(metricsConfig, instance), localRaidSettings);

            Singleton<AbstractGame>.Create(coopGame);
            metricsEvents.SetGameCreated();
            FikaEventDispatcher.DispatchEvent(new AbstractGameCreatedEvent(coopGame));

            if (!isServer)
            {
                coopGame.SetMatchmakerStatus("Coop game joined");
            }
            else
            {
                coopGame.SetMatchmakerStatus("Coop game created");
            }

            await coopGame.InitPlayer(raidSettings.BotSettings, new Callback(startHandler.HandleLoadComplete));
        }

        private class StartHandler(TarkovApplication tarkovApplication, Profile pmcProfile, Profile scavProfile, LocationSettingsClass.Location location, MatchmakerTimeHasCome.TimeHasComeScreenClass timeHasComeScreenController)
        {
            private readonly TarkovApplication tarkovApplication = tarkovApplication;
            private readonly Profile pmcProfile = pmcProfile;
            private readonly Profile scavProfile = scavProfile;
            private readonly LocationSettingsClass.Location location = location;
            private readonly MatchmakerTimeHasCome.TimeHasComeScreenClass timeHasComeScreenController = timeHasComeScreenController;

            public void HandleStop(Result<ExitStatus, TimeSpan, MetricsClass> result)
            {
                tarkovApplication.method_50(pmcProfile.Id, scavProfile, location, result, timeHasComeScreenController);
            }

            public void HandleLoadComplete(IResult error)
            {
                using (CounterCreatorAbstractClass.StartWithToken("LoadingScreen.LoadComplete"))
                {
                    GameObject.DestroyImmediate(MonoBehaviourSingleton<MenuUI>.Instance.gameObject);
                    MainMenuController mmc = (MainMenuController)typeof(TarkovApplication).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.FieldType == typeof(MainMenuController)).FirstOrDefault().GetValue(tarkovApplication);
                    mmc?.Unsubscribe();
                    GameWorld gameWorld = Singleton<GameWorld>.Instance;
                    gameWorld.OnGameStarted();
                    FikaEventDispatcher.DispatchEvent(new GameWorldStartedEvent(gameWorld));
                }
            }
        }
    }
}
