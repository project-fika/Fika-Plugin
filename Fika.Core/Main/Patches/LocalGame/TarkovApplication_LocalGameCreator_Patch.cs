using Diz.Resources;
using EFT.Ballistics;
using EFT.Communications;
using EFT.UI.Matchmaker;
using EFT.Utilities;
using JsonType;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.UI.Custom;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.SinglePlayer.Utils.InRaid;

namespace Fika.Core.Main.Patches.LocalGame;

/// <summary>
/// Created by: Paulov
/// Paulov: Overwrite and use our own CoopGame instance instead
/// </summary>
public sealed class TarkovApplication_LocalGameCreator_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TarkovApplication)
            .GetMethod(nameof(TarkovApplication.LocalGameCreate));
    }

    [PatchPrefix]
    public static bool Prefix(ref Task __result, TarkovApplication __instance, TimeAndWeatherSettings timeAndWeather,
        RaidSettings ____raidSettings, InputTree ____inputTree, GameDateTime ____localGameDateTime,
        float ____fixedDeltaTime, ClientMetricsEvents metricsEvents,
        ClientMetricsConfig metricsConfig, GameWorld gameWorld, MainMenuShowOperation ____menuOperation,
        CompositeDisposable ____unsubscriber, BundleLock ___BundleLock)
    {
#if DEBUG
        Logger.LogInfo("TarkovApplication_LocalGameCreator_Patch:Prefix");

#endif
        __result = CreateFikaGame(__instance, timeAndWeather, ____raidSettings,
            ____inputTree, ____localGameDateTime, ____fixedDeltaTime,
            metricsEvents, metricsConfig, gameWorld, ____menuOperation,
            ____unsubscriber, ___BundleLock);
        return false;
    }

    public static async Task CreateFikaGame(TarkovApplication instance, TimeAndWeatherSettings timeAndWeather,
        RaidSettings raidSettings, InputTree inputTree, GameDateTime localGameDateTime, float fixedDeltaTime,
        ClientMetricsEvents metricsEvents, ClientMetricsConfig metricsConfig,
        GameWorld gameWorld, MainMenuShowOperation menuOperation,
        CompositeDisposable unsubscriber, BundleLock bundleLock)
    {
        var isServer = FikaBackendUtils.IsServer;
        var isTransit = FikaBackendUtils.IsTransit;

        if (FikaPlugin.Instance.Settings.NoAI.Value)
        {
            FikaGlobals.LogWarning("No AI enabled - stopping bot spawns");
            raidSettings.BotSettings.BotAmount = EFT.Bots.EBotAmount.NoBots;
            raidSettings.WavesSettings.BotAmount = EFT.Bots.EBotAmount.NoBots;
            raidSettings.WavesSettings.IsBosses = false;
        }

        if (isServer && !isTransit)
        {
            FikaBackendUtils.CachedRaidSettings = raidSettings;
        }
        else if (isServer && isTransit && FikaBackendUtils.CachedRaidSettings != null)
        {
            Logger.LogInfo("Applying cached raid settings from previous raid");
            var cachedSettings = FikaBackendUtils.CachedRaidSettings;
            raidSettings.WavesSettings = cachedSettings.WavesSettings;
            raidSettings.BotSettings = cachedSettings.BotSettings;
            raidSettings.MetabolismDisabled = cachedSettings.MetabolismDisabled;
            raidSettings.PlayersSpawnPlace = cachedSettings.PlayersSpawnPlace;
        }

        metricsEvents.SetGamePrepared();

        if (Singleton<NotificationManager>.Instantiated)
        {
            Singleton<NotificationManager>.Instance.Deactivate();
        }

        var session = instance.Session;

        if (session == null)
        {
            throw new NullReferenceException("Backend session was null when initializing game!");
        }

        var profile = session.GetProfileBySide(raidSettings.Side);

        profile.Inventory.Stash = null;
        profile.Inventory.QuestStashItems = null;
        profile.Inventory.DiscardLimits = Singleton<ItemFactory>.Instance.GetDiscardLimits();

#if DEBUG
        Logger.LogInfo("TarkovApplication_LocalGameCreator_Patch:Postfix: Attempt to set Raid Settings");
        Logger.LogInfo($"RaidSettings Location: {raidSettings.LocationId}, TransitType: {raidSettings.transitionType}");
#endif

        if (!isServer)
        {
#if DEBUG
            Logger.LogInfo("Waiting for host to receive location");
#endif
            await WaitForServerToReceiveLocation();
#if DEBUG
            Logger.LogInfo("Host has received location, continuing");
#endif
        }

        if (!raidSettings.isInTransition)
        {
            await session.SendRaidSettings(raidSettings);
        }
        LocalRaidSettings localRaidSettings = new()
        {
            location = raidSettings.LocationId,
            timeVariant = raidSettings.SelectedDateTime,
            mode = ELocalMode.PVE_OFFLINE,
            playerSide = raidSettings.Side,
            transitionType = raidSettings.transitionType
        };
        var applicationTraverse = Traverse.Create(instance);
        applicationTraverse.Field<LocalRaidSettings>("_localRaidSettings").Value = localRaidSettings;

        var localSettings = await instance.Session.LocalRaidStarted(localRaidSettings);
        raidSettings.BotSettings.ExcludedBosses = localSettings.excludedBosses;
        var raidSettingsToUpdate = applicationTraverse.Field<LocalRaidSettings>("_localRaidSettings").Value;
        var escapeTimeLimit = raidSettings.IsScav ? RaidChangesUtil.NewEscapeTimeMinutes : raidSettings.SelectedLocation.EscapeTimeLimit;
        raidSettings.SelectedLocation = localSettings.locationLoot;
        raidSettings.SelectedLocation.EscapeTimeLimit = escapeTimeLimit;
        raidSettingsToUpdate.serverId = localSettings.serverId;
        raidSettingsToUpdate.selectedLocation = localSettings.locationLoot;
        raidSettingsToUpdate.selectedLocation.EscapeTimeLimit = escapeTimeLimit;

        var transitData = FikaBackendUtils.TransitData;
        transitData.transitionType = raidSettings.transitionType;
        raidSettingsToUpdate.transition = FikaBackendUtils.TransitData;

        var profileInsurance = localSettings.profileInsurance;
        if ((profileInsurance?.insuredItems) != null)
        {
            profile.InsuredItems = localSettings.profileInsurance.insuredItems;
        }

        if (!isServer)
        {
            instance.Matchmaker.UpdateMatchingStatus("Joining coop game...");

            RaidSettingsRequest data = new();
            var raidSettingsResponse = await FikaRequestHandler.GetRaidSettings(data);

            if (!raidSettingsResponse.Received)
            {
                throw new InvalidDataException("Failed to retrieve raid settings");
            }

            raidSettings.MetabolismDisabled = raidSettingsResponse.MetabolismDisabled;
            raidSettings.PlayersSpawnPlace = raidSettingsResponse.PlayersSpawnPlace;
            timeAndWeather.HourOfDay = raidSettingsResponse.HourOfDay;
            timeAndWeather.TimeFlowType = raidSettingsResponse.TimeFlowType;

            FikaBackendUtils.CustomRaidSettings = raidSettingsResponse.CustomRaidSettings;
            Logger.LogInfo($"Received CustomRaidSettings: {raidSettingsResponse.CustomRaidSettings}");
        }
        else
        {
            instance.Matchmaker.UpdateMatchingStatus("Hosting coop game...");
            Singleton<FikaServer>.Instance.LocationReceived = true;
        }

        // This gets incorrectly reset by the server, update it manually here during transit
        if (!FikaBackendUtils.IsHeadless && isTransit && MainMenuUIScript.Exist)
        {
            MainMenuUIScript.Instance.UpdatePresence(UI.FikaUIGlobals.EFikaPlayerPresence.IN_RAID);
        }

        StartHandler startHandler = new(instance, session.Profile, session.ProfileOfPet, raidSettings.SelectedLocation);

        var raidLimits = GetRaidMinutes(raidSettings.SelectedLocation.EscapeTimeLimit);

        var coopGame = CoopGame.Create(inputTree, profile, gameWorld, localGameDateTime, instance.Session.InsuranceCompany,
            MonoBehaviourSingleton<GameUI>.Instance, raidSettings.SelectedLocation,
            timeAndWeather, raidSettings.WavesSettings, raidSettings.SelectedDateTime, startHandler.HandleStop,
            fixedDeltaTime, instance.PlayerUpdateQueue, instance.Session, raidLimits, metricsEvents,
            new ClientMetricsCollector(metricsConfig, instance), localRaidSettings, raidSettings);

        startHandler.CoopGame = coopGame;

        Singleton<AbstractGame>.Create(coopGame);
        unsubscriber.AddDisposable(coopGame);
        unsubscriber.AddDisposable(startHandler.ReleaseSingleton);
        metricsEvents.SetGameCreated();
        FikaEventDispatcher.DispatchEvent(new AbstractGameCreatedEvent(coopGame));

        ScreenUpdater updater = new(instance.Matchmaker, coopGame);
        if (!isServer)
        {
            coopGame.SetMatchmakerStatus("Coop game joined");
        }
        else
        {
            coopGame.SetMatchmakerStatus("Coop game created");
        }

        await coopGame.InitPlayer(raidSettings.BotSettings);
        UnityEngine.Object.DestroyImmediate(MonoBehaviourSingleton<MenuUI>.Instance.gameObject);
        menuOperation?.Unsubscribe();
        bundleLock.MaxConcurrentOperations = FikaPlugin.Instance.Settings.MaxBundleLock.Value;
        gameWorld.OnGameStarted();
        updater.Dispose();

        if (FikaBackendUtils.IsSpectator)
        {
            Logger.LogInfo("Starting game as spectator");
            await HandleJoinAsSpectator();
        }
    }

    private static async Task WaitForServerToReceiveLocation()
    {
        var packet = new InformationPacket();
        var client = Singleton<FikaClient>.Instance;
        var span = TimeSpan.FromSeconds(1);

        do
        {
            client.SendData(ref packet, DeliveryMethod.ReliableUnordered);
            await Task.Delay(span);
        } while (!client.HostReceivedLocation);
    }

    private static TimeSpan GetRaidMinutes(int defaultMinutes)
    {
        return TimeSpan.FromSeconds(60 * defaultMinutes);
    }

    private class StartHandler(TarkovApplication tarkovApplication, Profile pmcProfile, Profile scavProfile,
        LocationSettings.Location location)
    {
        private readonly TarkovApplication _tarkovApplication = tarkovApplication;
        private readonly Profile _pmcProfile = pmcProfile;
        private readonly Profile _scavProfile = scavProfile;
        private readonly LocationSettings.Location _location = location;
        public CoopGame CoopGame;

        public void HandleStop(Result<ExitStatus, TimeSpan, ClientMetrics> result)
        {
            _tarkovApplication.OnGameEnd(_pmcProfile.Id, _scavProfile, _location, result);
        }

        public void ReleaseSingleton()
        {
            Singleton<AbstractGame>.Release(CoopGame);
            Singleton<IFikaGame>.Release(CoopGame);
        }
    }

    private static async Task HandleJoinAsSpectator()
    {
        var MainPlayer = Singleton<GameWorld>.Instance.MainPlayer;

        // Teleport the player underground to avoid it from being looted
        var currentPosition = MainPlayer.Position;
        MainPlayer.Teleport(new(currentPosition.x, currentPosition.y - 75, currentPosition.z));

        // Small delay to ensure the teleport command is processed first
        await Task.Delay(250);

        DamageInfo damageInfo = new()
        {
            Damage = 1000,
            DamageType = EDamageType.Impact
        };

        // Kill the player to put it in spectator mode
        MainPlayer.ApplyDamageInfo(damageInfo, EBodyPart.Head, EBodyPartColliderType.Eyes, 0);
    }
}

internal class ScreenUpdater : IDisposable
{
    private readonly MatchmakerPlayersController _matchmakerPlayerControllerClass;
    private readonly CoopGame _coopGame;

    public ScreenUpdater(MatchmakerPlayersController controller, CoopGame game)
    {
        _matchmakerPlayerControllerClass = controller;
        _coopGame = game;
        game.OnMatchingStatusChanged += UpdateStatus;
    }

    private void UpdateStatus(string text, float? progress)
    {
        _matchmakerPlayerControllerClass.UpdateMatchingStatus(text, progress);
    }

    public void Dispose()
    {
        _coopGame.OnMatchingStatusChanged -= UpdateStatus;
    }
}
