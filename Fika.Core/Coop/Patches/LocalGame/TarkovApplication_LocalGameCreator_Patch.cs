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
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fika.Core.Networking;
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

        static ISession CurrentSession { get; set; }

        [PatchPrefix]
        public static bool Prefix(TarkovApplication __instance)
        {
            Logger.LogDebug("TarkovApplication_LocalGameCreator_Patch:Prefix");

            if (FikaBackendUtils.IsSinglePlayer)
            {
                return true;
            }

            ISession session = __instance.GetClientBackEndSession();
            if (session == null)
            {
                Logger.LogError("Session is NULL. Continuing as Single-player.");
                return true;
            }

            CurrentSession = session;

            return false;
        }

        [PatchPostfix]
        public static async Task Postfix(Task __result, TarkovApplication __instance, TimeAndWeatherSettings timeAndWeather, MatchmakerTimeHasCome.TimeHasComeScreenClass timeHasComeScreenController,
            RaidSettings ____raidSettings, InputTree ____inputTree, GameDateTime ____localGameDateTime, float ____fixedDeltaTime, string ____backendUrl)
        {
            if (FikaBackendUtils.IsSinglePlayer)
            {
                return;
            }

            if (CurrentSession == null)
            {
                return;
            }

            if (____raidSettings == null)
            {
                Logger.LogError("RaidSettings is Null");
                throw new ArgumentNullException("RaidSettings");
            }

            if (timeHasComeScreenController == null)
            {
                Logger.LogError("timeHasComeScreenController is Null");
                throw new ArgumentNullException("timeHasComeScreenController");
            }

            bool isServer = FikaBackendUtils.IsServer;

            LocationSettingsClass.Location location = ____raidSettings.SelectedLocation;

            FikaBackendUtils.ScreenController = timeHasComeScreenController;

            if (Singleton<NotificationManagerClass>.Instantiated)
            {
                Singleton<NotificationManagerClass>.Instance.Deactivate();
            }

            NetManagerUtils.CreateNetManager(FikaBackendUtils.IsServer);
            if (isServer)
            {
                NetManagerUtils.StartPinger();
            }

            ISession session = CurrentSession;

            Profile profile = session.GetProfileBySide(____raidSettings.Side);

            profile.Inventory.Stash = null;
            profile.Inventory.QuestStashItems = null;
            profile.Inventory.DiscardLimits = Singleton<ItemFactory>.Instance.GetDiscardLimits();

            Logger.LogDebug("TarkovApplication_LocalGameCreator_Patch:Postfix: Attempt to set Raid Settings");

            await session.SendRaidSettings(____raidSettings);

            if (!isServer)
            {
                timeHasComeScreenController.ChangeStatus("Connecting to host...");

                var attempts = 0;
                var delay = 500; // ms
                var maxAttempts = 600; // 5 minutes
                var connected = false;
                
                do
                {
                    try
                    {
                        GetHostRequest body = new(FikaBackendUtils.GetServerId());
                        GetHostResponse result = FikaRequestHandler.GetHost(body);

                        if (result.Ips is { Length: > 0 })
                        {
                            connected = true;
                            break;
                        }
                    }
                    catch
                    {
                        // keep trying
                    }

                    await Task.Delay(delay); // try every half second for 5 minutes
                    attempts++;
                } while (attempts < maxAttempts);

                if (!connected)
                {
                    timeHasComeScreenController.method_7(); // Abort
                    
                    Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(
                        "ERROR CONNECTING",
                        "Unable to connect to the host. Make sure they are still in-raid.",
                        ErrorScreen.EButtonType.OkButton, 10f, null, null);

                    FikaPlugin.Instance.FikaLogger.LogError("Unable to connect to the host");
                    return;
                }

                NetManagerUtils.CreatePingingClient();

                var pingingClient = Singleton<FikaPingingClient>.Instance;

                if (pingingClient.Init(FikaBackendUtils.GetServerId()))
                {
                    attempts = 0;
                    bool success;
                    
                    do
                    {
                        attempts++;
                        
                        pingingClient.PingEndPoint("fika.hello");
                        pingingClient.NetClient.PollEvents();
                        success = pingingClient.Received;

                        await Task.Delay(100);
                    } while (!success && attempts < 50);

                    if (!success)
                    {
                        timeHasComeScreenController.method_7(); // Abort
                        
                        Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(
                            "ERROR CONNECTING",
                            "Unable to connect to the server. Make sure that all ports are open and that all settings are configured correctly.",
                            ErrorScreen.EButtonType.OkButton, 10f, null, null);

                        FikaPlugin.Instance.FikaLogger.LogError("Unable to connect to the session!");
                        
                        NetManagerUtils.DestroyPingingClient();

                        return;
                    }
                }
                
                if (FikaBackendUtils.IsHostNatPunch)
                {
                    pingingClient.StartKeepAliveRoutine();
                }
                else
                {
                    NetManagerUtils.DestroyPingingClient();
                }
                
                timeHasComeScreenController.ChangeStatus("Joining coop game...");

                RaidSettingsRequest data = new();
                RaidSettingsResponse raidSettingsResponse = await FikaRequestHandler.GetRaidSettings(data);

                ____raidSettings.MetabolismDisabled = raidSettingsResponse.MetabolismDisabled;
                ____raidSettings.PlayersSpawnPlace = (EPlayersSpawnPlace)Enum.Parse(typeof(EPlayersSpawnPlace), raidSettingsResponse.PlayersSpawnPlace);
            }
            else
            {
                timeHasComeScreenController.ChangeStatus("Creating coop game...");
            }

            StartHandler startHandler = new(__instance, session.Profile, session.ProfileOfPet, ____raidSettings.SelectedLocation, timeHasComeScreenController);

            TimeSpan raidLimits = __instance.method_48(____raidSettings.SelectedLocation.EscapeTimeLimit);

            CoopGame coopGame = CoopGame.Create(____inputTree, profile, ____localGameDateTime, session.InsuranceCompany, MonoBehaviourSingleton<MenuUI>.Instance, MonoBehaviourSingleton<GameUI>.Instance,
                ____raidSettings.SelectedLocation, timeAndWeather, ____raidSettings.WavesSettings, ____raidSettings.SelectedDateTime, new Callback<ExitStatus, TimeSpan, MetricsClass>(startHandler.HandleStop),
                ____fixedDeltaTime, EUpdateQueue.Update, session, raidLimits, ____raidSettings);

            Singleton<AbstractGame>.Create(coopGame);
            FikaEventDispatcher.DispatchEvent(new AbstractGameCreatedEvent(coopGame));

            if (!isServer)
            {
                coopGame.SetMatchmakerStatus("Coop game joined");
            }
            else
            {
                coopGame.SetMatchmakerStatus("Coop game created");
            }

            Task finishTask = coopGame.InitPlayer(____raidSettings.BotSettings, ____backendUrl, new Callback(startHandler.HandleLoadComplete));
            __result = Task.WhenAll(finishTask);
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
                    UnityEngine.Object.DestroyImmediate(MonoBehaviourSingleton<MenuUI>.Instance.gameObject);
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
