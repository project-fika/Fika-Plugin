using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Matchmaker;
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

            if (MatchmakerAcceptPatches.IsSinglePlayer)
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
        public static async Task Postfix(Task __result, TarkovApplication __instance, TimeAndWeatherSettings timeAndWeather, MatchmakerTimeHasCome.GClass3186 timeHasComeScreenController,
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

            bool isServer = MatchmakerAcceptPatches.IsServer;

            LocationSettingsClass.Location location = ____raidSettings.SelectedLocation;

            MatchmakerAcceptPatches.GClass3186 = timeHasComeScreenController;

            if (Singleton<NotificationManagerClass>.Instantiated)
            {
                Singleton<NotificationManagerClass>.Instance.Deactivate();
            }

            NetManagerUtils.CreateNetManager(MatchmakerAcceptPatches.IsServer);
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

        private class StartHandler(TarkovApplication tarkovApplication, Profile pmcProfile, Profile scavProfile, LocationSettingsClass.Location location, MatchmakerTimeHasCome.GClass3186 timeHasComeScreenController)
        {
            private readonly TarkovApplication tarkovApplication = tarkovApplication;
            private readonly Profile pmcProfile = pmcProfile;
            private readonly Profile scavProfile = scavProfile;
            private readonly LocationSettingsClass.Location location = location;
            private readonly MatchmakerTimeHasCome.GClass3186 timeHasComeScreenController = timeHasComeScreenController;

            public void HandleStop(Result<ExitStatus, TimeSpan, MetricsClass> result)
            {
                tarkovApplication.method_50(pmcProfile.Id, scavProfile, location, result, timeHasComeScreenController);
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
