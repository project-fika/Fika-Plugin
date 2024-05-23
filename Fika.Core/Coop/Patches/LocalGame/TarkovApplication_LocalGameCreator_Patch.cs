using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Patches.LocalGame
{
    /// <summary>
    /// Created by: Paulov
    /// Paulov: Overwrite and use our own CoopGame instance instead
    /// </summary>
    internal class TarkovApplication_LocalGameCreator_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_43));

        static ISession CurrentSession { get; set; }

        [PatchPrefix]
        public static bool Prefix(TarkovApplication __instance)
        {
            Logger.LogDebug("TarkovApplication_LocalGameCreator_Patch:Prefix");

            if (MatchmakerAcceptPatches.IsSinglePlayer)
                return true;

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
        public static async Task Postfix(Task __result, TarkovApplication __instance, TimeAndWeatherSettings timeAndWeather, MatchmakerTimeHasCome.GClass3163 timeHasComeScreenController,
            RaidSettings ____raidSettings, InputTree ____inputTree, GameDateTime ____localGameDateTime, float ____fixedDeltaTime, string ____backendUrl)
        {
            if (MatchmakerAcceptPatches.IsSinglePlayer)
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

            LocationSettingsClass.Location location = ____raidSettings.SelectedLocation;

            MatchmakerAcceptPatches.GClass3163 = timeHasComeScreenController;

            if (Singleton<NotificationManagerClass>.Instantiated)
            {
                Singleton<NotificationManagerClass>.Instance.Deactivate();
            }

            ISession session = CurrentSession;

            Profile profile = session.GetProfileBySide(____raidSettings.Side);

            profile.Inventory.Stash = null;
            profile.Inventory.QuestStashItems = null;
            profile.Inventory.DiscardLimits = Singleton<ItemFactory>.Instance.GetDiscardLimits();

            Logger.LogDebug("TarkovApplication_LocalGameCreator_Patch:Postfix: Attempt to set Raid Settings");

            await session.SendRaidSettings(____raidSettings);

            if (MatchmakerAcceptPatches.IsClient)
            {
                timeHasComeScreenController.ChangeStatus("Joining Coop Game");

                RaidSettingsRequest data = new();
                RaidSettingsResponse raidSettingsResponse = await FikaRequestHandler.GetRaidSettings(data);

                ____raidSettings.MetabolismDisabled = raidSettingsResponse.MetabolismDisabled;
                ____raidSettings.PlayersSpawnPlace = (EPlayersSpawnPlace)Enum.Parse(typeof(EPlayersSpawnPlace), raidSettingsResponse.PlayersSpawnPlace);
            }
            else
            {
                timeHasComeScreenController.ChangeStatus("Creating Coop Game");
            }

            StartHandler startHandler = new(__instance, session.Profile, session.ProfileOfPet, ____raidSettings.SelectedLocation, timeHasComeScreenController);

            CoopGame localGame = CoopGame.Create(____inputTree, profile, ____localGameDateTime,
                session.InsuranceCompany, MonoBehaviourSingleton<MenuUI>.Instance,
                MonoBehaviourSingleton<CommonUI>.Instance, MonoBehaviourSingleton<PreloaderUI>.Instance,
                MonoBehaviourSingleton<GameUI>.Instance, ____raidSettings.SelectedLocation, timeAndWeather,
                ____raidSettings.WavesSettings, ____raidSettings.SelectedDateTime, new Callback<ExitStatus, TimeSpan, MetricsClass>(startHandler.HandleStart),
                ____fixedDeltaTime, EUpdateQueue.Update, session, TimeSpan.FromSeconds(60 * ____raidSettings.SelectedLocation.EscapeTimeLimit), ____raidSettings
            );

            Singleton<AbstractGame>.Create(localGame);
            FikaEventDispatcher.DispatchEvent(new AbstractGameCreatedEvent(localGame));

            if (MatchmakerAcceptPatches.IsClient)
            {
                timeHasComeScreenController.ChangeStatus("Joined Coop Game");
            }
            else
            {
                timeHasComeScreenController.ChangeStatus("Created Coop Game");
            }

            Task finishTask = localGame.InitPlayer(____raidSettings.BotSettings, ____backendUrl, null, new Callback(startHandler.HandleLoadComplete));
            __result = Task.WhenAll(finishTask);
        }

        private class StartHandler(TarkovApplication tarkovApplication, Profile pmcProfile, Profile scavProfile, LocationSettingsClass.Location location, MatchmakerTimeHasCome.GClass3163 timeHasComeScreenController)
        {
            private readonly TarkovApplication tarkovApplication = tarkovApplication;
            private readonly Profile pmcProfile = pmcProfile;
            private readonly Profile scavProfile = scavProfile;
            private readonly LocationSettingsClass.Location location = location;
            private readonly MatchmakerTimeHasCome.GClass3163 timeHasComeScreenController = timeHasComeScreenController;

            public void HandleStart(Result<ExitStatus, TimeSpan, MetricsClass> result)
            {
                tarkovApplication.method_46(pmcProfile.Id, scavProfile, location, result, timeHasComeScreenController);
            }

            public void HandleLoadComplete(IResult error)
            {
                using (GClass21.StartWithToken("LoadingScreen.LoadComplete"))
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
