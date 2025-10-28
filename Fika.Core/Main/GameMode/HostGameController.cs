﻿using Comfort.Common;
using CommonAssets.Scripts.Game.LabyrinthEvent;
using EFT;
using EFT.AssetsManager;
using EFT.Bots;
using EFT.Counters;
using EFT.Game.Spawning;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.Weather;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.Components;
using Fika.Core.Main.FreeCamera;
using Fika.Core.Main.HostClasses;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Patches.Overrides;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Networking.Pooling;
using Fika.Core.UI.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using static LocationSettingsClass;

namespace Fika.Core.Main.GameMode;

public class HostGameController : BaseGameController, IBotGame
{
    public HostGameController(IFikaGame game, EUpdateQueue updateQueue, GameWorld gameWorld, ISession session,
        LocationSettingsClass.Location location, WavesSettings wavesSettings, GameDateTime gameDateTime)
        : base(game, updateQueue, gameWorld, session)
    {
        _botsController = new();
        _botStateManager = BotStateManager.Create(_abstractGame, Singleton<FikaServer>.Instance, this);
        _gameDateTime = gameDateTime;

        if (game is not AbstractGame abstractGame)
        {
            throw new NullReferenceException("Missing AbstractGame");
        }

        // Non Waves Scenario setup
        _nonWavesSpawnScenario = NonWavesSpawnScenario.smethod_0(abstractGame, location, _botsController);
        _nonWavesSpawnScenario.ImplementWaveSettings(wavesSettings);

        // Waves Scenario setup
        WildSpawnWave[] waves = LocalGame.smethod_7(wavesSettings, location.waves);
        _wavesSpawnScenario = WavesSpawnScenario.smethod_0(abstractGame.gameObject, waves, _botsController.ActivateBotsByWave, location);

        // Boss Scenario setup
        BossLocationSpawn[] bossSpawns = LocalGame.smethod_8(true, wavesSettings, location.BossLocationSpawn);
        _bossSpawnScenario = BossSpawnScenario.smethod_0(bossSpawns, _botsController.ActivateBotsByWave);
    }

    public byte[] LootData { get; set; }

    protected readonly BotStateManager _botStateManager;
    protected readonly NonWavesSpawnScenario _nonWavesSpawnScenario;
    protected readonly BotsController _botsController;
    protected readonly WavesSpawnScenario _wavesSpawnScenario;
    protected readonly BossSpawnScenario _bossSpawnScenario;
    protected readonly Dictionary<int, int> _botQueue = [];
    protected GameDateTime _gameDateTime;

    /// <summary>
    /// How long in seconds until a bot is force spawned if not every client could load it
    /// </summary>
    private readonly float _botTimeout = 120f;
    /// <summary>
    /// The <see cref="Task.Delay(int)"/> in every loop during the <see cref="_botTimeout"/>
    /// </summary>
    private readonly float _botTimeoutDelay = 0.25f;

    public GameStatus Status
    {
        get
        {
            return _abstractGame.Status;
        }
    }

    public GameDateTime GameDateTime
    {
        get
        {
            return _gameDateTime;
        }
        set
        {
            _gameDateTime = value;
        }
    }

    public BotsController BotsController
    {
        get
        {
            return _botsController;
        }
    }

    public IWeatherCurve WeatherCurve
    {
        get
        {
            return WeatherController.Instance.WeatherCurve;
        }
    }

    public BossSpawnScenario BossSpawnScenario
    {
        get
        {
            return _bossSpawnScenario;
        }
    }

    public event Action UpdateByUnity;

    public Action Update
    {
        get
        {
            return UpdateByUnity;
        }
    }

    public override Task ReceiveSpawnPoint(Profile profile)
    {
        throw new NotImplementedException("Should not be called as a host");
    }

    public void BotDespawn(BotOwner bot)
    {
        Player getPlayer = bot.GetPlayer;
        _botsController.BotDied(bot);
        _botsController.DestroyInfo(getPlayer);
        AssetPoolObject.ReturnToPool(bot.gameObject, true);
    }

    public override IEnumerator WaitForHostInit(int timeBeforeDeployLocal)
    {
        if (_fikaGame is not AbstractGame abstractGame)
        {
            throw new NullReferenceException("AbstractGame was missing");
        }

        FikaServer server = Singleton<FikaServer>.Instance;
        server.HostReady = true;

        DateTime startTime = EFTDateTimeClass.UtcNow.AddSeconds((double)timeBeforeDeployLocal);
        GameTime = startTime;
        server.GameStartTime = startTime;
        SessionTime = abstractGame.GameTimer.SessionTime;

        InformationPacket packet = new()
        {
            RaidStarted = RaidStarted,
            ReadyPlayers = server.ReadyClients,
            HostReady = server.HostReady,
            GameTime = GameTime.Value,
            SessionTime = SessionTime.Value
        };

        server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        LootData = null;

        yield break;
    }

    #region Bots
    /// <summary>
    /// Used to spawn a bot for the host
    /// </summary>
    /// <param name="profile"><see cref="Profile"/> to spawn</param>
    /// <param name="position">The <see cref="Vector3"/> position to spawn on</param>
    /// <returns></returns>
    private async Task<LocalPlayer> CreateBot(GameWorld gameWorld, Profile profile, Vector3 position)
    {
#if DEBUG
        Logger.LogWarning($"Creating bot {profile.Info.Settings.Role} at {position}");
#endif

        if (_coopHandler == null)
        {
            Logger.LogError($"{nameof(CreateBot)}: CoopHandler was null");
            return null;
        }

        int netId = 1000;
        FikaBot fikaBot;
        if (Bots.ContainsKey(profile.Id))
        {
            return null;
        }

        profile.SetSpawnedInSession(profile.Info.Side == EPlayerSide.Savage);

        FikaServer server = Singleton<FikaServer>.Instance;
        netId = server.PopNetId();

        MongoID mongoId = MongoID.Generate(true);
        const ushort nextOperationId = 0;
        SendCharacterPacket packet = SendCharacterPacket.FromValue(new()
        {
            Profile = profile,
            ControllerId = mongoId,
            FirstOperationId = nextOperationId,
            IsZombie = profile.Info.Settings.UseSimpleAnimator
        }, true, true, position, netId);
        packet.PlayerInfoPacket.HealthByteArray = profile.Health.SerializeHealthInfo();
        server.SendGenericPacket(EGenericSubPacketType.SendCharacter, packet, true);

        if (server.NetServer.ConnectedPeersCount > 0)
        {
            await WaitForPlayersToLoadBotProfile(netId);
        }

        if (profile.Info.Side is not EPlayerSide.Savage)
        {
            var backpack = profile.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;
            if (backpack != null)
            {
                foreach (var backpackItem in backpack.GetAllItems())
                {
                    if (backpackItem != backpack)
                    {
                        backpackItem.SpawnedInSession = true;
                    }
                }
            }

            // We still want DogTags to be 'FiR'
            var item = profile.Inventory.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem;
            if (item != null)
            {
                item.SpawnedInSession = true;
            }
        }

        fikaBot = await FikaBot.CreateBot(_gameWorld, netId, position, Quaternion.identity, "Player",
           "Bot_", EPointOfView.ThirdPerson, profile, true, _updateQueue, Player.EUpdateMode.Auto,
           Player.EUpdateMode.Auto, BackendConfigAbstractClass.Config.CharacterController.BotPlayerMode, FikaGlobals.GetOtherPlayerSensitivity,
            FikaGlobals.GetOtherPlayerSensitivity, ObservedViewFilter.Default, mongoId, nextOperationId);

        fikaBot.Location = Location.Id;
        Bots.Add(fikaBot.ProfileId, fikaBot);

        if (FikaPlugin.DisableBotMetabolism.Value)
        {
            fikaBot.HealthController.DisableMetabolism();
        }
        _coopHandler.Players.Add(fikaBot.NetId, fikaBot);

        if (profile.Info.Settings.Role != WildSpawnType.shooterBTR)
        {
            _botStateManager.AddBot(fikaBot);
            var spawnPacket = SpawnAI.FromValue(netId, position);
            server.SendGenericPacket(EGenericSubPacketType.SpawnAI, spawnPacket);
        }

        return fikaBot;
    }

    /// <summary>
    /// Increments the amount of players that have loaded a bot, used for <see cref="WaitForPlayersToLoadBotProfile(int)"/>
    /// </summary>
    /// <param name="netId">The id to increment</param>
    public void IncreaseLoadedPlayers(int netId)
    {
        if (_botQueue.ContainsKey(netId))
        {
            _botQueue[netId]++;
        }
        else
        {
            Logger.LogError($"IncreaseLoadedPlayers::Could not find netId {netId}!");
        }
    }

    /// <summary>
    /// <see cref="Task"/> used to ensure that all players loads a bot before it spawns
    /// </summary>
    /// <param name="netId">The NetId to spawn</param>
    /// <returns></returns>
    private async Task WaitForPlayersToLoadBotProfile(int netId)
    {
        _botQueue.Add(netId, 0);
        var server = Singleton<FikaServer>.Instance;
        var connectedPeers = server.NetServer.ConnectedPeersCount;

        var elapsedSeconds = 0f;

        while (_botQueue[netId] < connectedPeers)
        {
            if (elapsedSeconds >= _botTimeout)
            {
                Logger.LogWarning($"WaitForPlayersToLoadBotProfile::Took too long for every player to load the bot with id {netId}, force spawning!");
                _botQueue.Remove(netId);
                return;
            }

            await Task.Delay((int)(_botTimeoutDelay * 1000)); // multiply 0.X * 1000 to get milliseconds
            elapsedSeconds += _botTimeoutDelay;
            connectedPeers = server.NetServer.ConnectedPeersCount;
        }

        if (elapsedSeconds > 30f)
        {
            Logger.LogWarning($"WaitForPlayersToLoadBotProfile::Bot [{netId}] took an abnormal amount of time to load: {elapsedSeconds:F1}s");
        }

        _botQueue.Remove(netId);
    }

    /// <summary>
    /// Despawns a bot
    /// </summary>
    /// <param name="coopHandler"></param>
    /// <param name="bot">The bot to despawn</param>
    internal void DespawnBot(CoopHandler coopHandler, Player bot)
    {
        BotOwner botOwner = bot.AIData.BotOwner;

        _botsController.Bots.Remove(botOwner);
        bot.HealthController.DiedEvent -= botOwner.method_6; // Unsubscribe from the event to prevent errors.
        BotDespawn(botOwner);
        if (botOwner != null)
        {
            botOwner.Dispose();
        }

        FikaPlayer fikaPlayer = (FikaPlayer)bot;
        coopHandler.Players.Remove(fikaPlayer.NetId);
        Bots.Remove(bot.ProfileId);
    }
    #endregion

    /// <summary>
    /// Sets up a custom weather curve
    /// </summary>
    /// <param name="timeAndWeather">Struct with custom settings</param>
    public void SetupCustomWeather(TimeAndWeatherSettings timeAndWeather)
    {
        if (WeatherController.Instance == null)
        {
            return;
        }

        DateTime dateTime = EFTDateTimeClass.StartOfDay();
        DateTime dateTime2 = dateTime.AddDays(1);

        WeatherClass weather = WeatherClass.CreateDefault();
        WeatherClass weather2 = WeatherClass.CreateDefault();
        weather.Cloudness = weather2.Cloudness = timeAndWeather.CloudinessType.ToValue();
        weather.Rain = weather2.Rain = timeAndWeather.RainType.ToValue();
        weather.Wind = weather2.Wind = timeAndWeather.WindType.ToValue();
        weather.ScaterringFogDensity = weather2.ScaterringFogDensity = timeAndWeather.FogType.ToValue();
        weather.Time = dateTime.Ticks;
        weather2.Time = dateTime2.Ticks;
        WeatherClass[] weatherClasses = [weather, weather2];
        WeatherClasses = weatherClasses;
        WeatherController.Instance.method_0(weatherClasses);
    }

    public override async Task StartBotSystemsAndCountdown(BotControllerSettings controllerSettings, GameWorld gameWorld)
    {
        if (Location.Id == "laboratory")
        {
            Logger.LogInfo("Location is 'Laboratory', skipping weather generation");
            Season = ESeason.Summer;
            OfflineRaidSettingsMenuPatch_Override.UseCustomWeather = false;
        }
        else
        {
            await GenerateWeathers();
        }

        gameWorld.RegisterRestrictableZones();
        gameWorld.RegisterBorderZones();

        await InitializeBotsSystem(Location, controllerSettings, gameWorld);

#if DEBUG
        Logger.LogWarning("Starting " + nameof(BaseGameController.WaitForOtherPlayersToLoad));
#endif
        await WaitForOtherPlayersToLoad();

        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_FINISHING_RAID_INIT.Localized());
        Logger.LogInfo("All players are loaded, continuing...");

        await StartBotsSystem(Location);

        // Add FreeCamController to GameWorld GameObject
        if (!FikaBackendUtils.IsHeadless)
        {
            FreeCameraController freeCamController = gameWorld.gameObject.AddComponent<FreeCameraController>();
            Singleton<FreeCameraController>.Create(freeCamController);
        }

        await SetupRaidCode();

        Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal = Math.Max(Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal, 3);
    }

    public override async Task WaitForHostToStart()
    {
        await base.WaitForHostToStart();

        GameObject startButton = null;
        if (!FikaBackendUtils.IsHeadless)
        {
            startButton = CreateStartButton() ?? throw new NullReferenceException("Start button could not be created!");
        }
        FikaServer server = Singleton<FikaServer>.Instance;
        server.RaidInitialized = true;

        if (FikaPlugin.DevMode.Value)
        {
            Logger.LogWarning("DevMode is enabled, skipping wait...");
            NotificationManagerClass.DisplayMessageNotification("DevMode enabled, starting automatically...", iconType: EFT.Communications.ENotificationIconType.Note);
            RaidStarted = true;
        }

        while (!RaidStarted)
        {
            await Task.Yield();
        }

        Logger.LogInfo("Raid has been started...");

        if (FikaPlugin.UseNatPunching.Value)
        {
            server.StopNatIntroduceRoutine();
        }

        if (startButton != null)
        {
            GameObject.Destroy(startButton);
        }

        InformationPacket continuePacket = new()
        {
            AmountOfPeers = server.NetServer.ConnectedPeersCount + 1
        };
        server.SendData(ref continuePacket, DeliveryMethod.ReliableOrdered);
        SetStatusModel status = new(FikaBackendUtils.GroupId, LobbyEntry.ELobbyStatus.IN_GAME);
        await FikaRequestHandler.UpdateSetStatus(status);
    }

    public override async Task WaitForOtherPlayersToLoad()
    {
        float expectedPlayers = Singleton<IFikaNetworkManager>.Instance.PlayerAmount;
        if (expectedPlayers <= 1)
        {
            Singleton<FikaServer>.Instance.ReadyClients++;
            return;
        }

        if (FikaBackendUtils.IsHeadless)
        {
            expectedPlayers--;
        }
#if DEBUG
        Logger.LogWarning("Server: Waiting for coopHandler.AmountOfHumans < expected players, expected: " + expectedPlayers);
#endif
        FikaServer server = Singleton<FikaServer>.Instance;
        server.ReadyClients++;
        do
        {
            await Task.Delay(100);
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized(), (float)_coopHandler.AmountOfHumans / expectedPlayers);
        } while (_coopHandler.AmountOfHumans < expectedPlayers);

        InformationPacket packet = new()
        {
            RaidStarted = RaidStarted,
            ReadyPlayers = server.ReadyClients
        };

        server.SendData(ref packet, DeliveryMethod.ReliableOrdered);

#if DEBUG
        Logger.LogWarning("Server: Waiting for server.ReadyClients < expected players, expected: " + expectedPlayers);
#endif
        if (FikaBackendUtils.IsHeadless)
        {
            expectedPlayers++;
        }

        do
        {
            await Task.Delay(100);
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized(), (float)server.ReadyClients / expectedPlayers);
        } while (server.ReadyClients < expectedPlayers);

        InformationPacket finalPacket = new()
        {
            RaidStarted = RaidStarted,
            ReadyPlayers = server.ReadyClients
        };

        server.SendData(ref finalPacket, DeliveryMethod.ReliableOrdered);
    }

    public override async Task GenerateWeathers()
    {
        if (WeatherController.Instance != null)
        {
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_INIT_WEATHER.Localized());
            Logger.LogInfo("Generating and initializing weather...");
            WeatherRequestClass weather = await _backendSession.WeatherRequest();
            Season = weather.Season;
            SeasonsSettings = weather.SeasonsSettings;
            if (!OfflineRaidSettingsMenuPatch_Override.UseCustomWeather)
            {
                WeatherClasses = weather.Weathers;
                WeatherController.Instance.method_0(WeatherClasses);
            }
        }
    }

    public override IEnumerator CountdownScreen(Profile profile, string profileId)
    {
        yield return base.CountdownScreen(profile, profileId);
        _localPlayer.PacketSender.Init();

        _gameWorld.StartCoroutine(SyncTraps());
        _gameWorld.StartCoroutine(CreateStashes());
    }

    public override void CreateSpawnSystem(Profile profile)
    {
        _spawnPoints = SpawnPointManagerClass.CreateFromScene(new DateTime?(EFTDateTimeClass.LocalDateTimeFromUnixTime(Location.UnixDateTime)),
                                Location.SpawnPointParams);
        int spawnSafeDistance = (Location.SpawnSafeDistanceMeters > 0) ? Location.SpawnSafeDistanceMeters : 100;
        SpawnSettingsStruct settings = new(Location.MinDistToFreePoint, Location.MaxDistToFreePoint, Location.MaxBotPerZone, spawnSafeDistance, Location.NoGroupSpawn, Location.OneTimeSpawn);
        SpawnSystem = SpawnSystemCreatorClass.CreateSpawnSystem(settings, FikaGlobals.GetApplicationTime, Singleton<GameWorld>.Instance, _botsController, _spawnPoints);
        _spawnPoint = SpawnSystem.SelectSpawnPoint(ESpawnCategory.Player, profile.Info.Side, null, null, null, null, profile.Id);
        InfiltrationPoint = string.IsNullOrEmpty(_spawnPoint.Infiltration) ? "MissingInfiltration" : _spawnPoint.Infiltration;
    }

    public async Task InitializeBotsSystem(LocationSettingsClass.Location location,
        BotControllerSettings controllerSettings, GameWorld gameWorld)
    {
        BotsPresets botsPresets = new(_backendSession, _wavesSpawnScenario.SpawnWaves,
                _bossSpawnScenario.BossSpawnWaves, _nonWavesSpawnScenario.GClass1879_0, false);
        List<WaveInfoClass> waveInfos = [];
        LocationSettingsClass.Location.GClass1425 halloween = location.Events.Halloween2024;
        if (halloween?.InfectionPercentage > 0)
        {
            waveInfos.AddRange(BotHalloweenWithZombies.GetProfilesOnStart());
        }
        await botsPresets.TryLoadBotsProfilesOnStart(waveInfos);
        BotCreatorClass botCreator = new(this, botsPresets, CreateBot);
        BotZone[] botZones = [.. LocationScene.GetAllObjects<BotZone>(false)];

        bool useWaveControl = controllerSettings.BotAmount == EBotAmount.Horde;

        if (FikaPlugin.NoAI.Value)
        {
            FikaGlobals.LogWarning("No AI enabled - stopping bot spawns");
            controllerSettings.BotAmount = EBotAmount.NoBots;
        }

        _botsController.Init(this, botCreator, botZones, SpawnSystem, _wavesSpawnScenario.BotLocationModifier,
            controllerSettings.IsEnabled, controllerSettings.IsScavWars, useWaveControl, false,
        _bossSpawnScenario.HaveSectants, gameWorld, location.OpenZones, location.Events);
        UpdateByUnity -= _botsController.method_0;
        if (controllerSettings.ExcludedBosses != null)
        {
            _botsController.BotSpawner.SetBlockedRoles(controllerSettings.ExcludedBosses);
        }
        _botStateManager.AssignBotsController(_botsController);

        int numberOfBots = controllerSettings.BotAmount switch
        {
            EBotAmount.AsOnline => _backendSession.BackEndConfig.Config.MaxBotsAliveOnMap,
            EBotAmount.NoBots => 0,
            EBotAmount.Low => 15,
            EBotAmount.Medium => 20,
            EBotAmount.High => 25,
            EBotAmount.Horde => 35,
            _ => 15,
        };

        _botsController.SetSettings(numberOfBots, _backendSession.BackEndConfig.BotPresets, _backendSession.BackEndConfig.BotWeaponScatterings);
        if (!FikaBackendUtils.IsHeadless)
        {
            if (Singleton<IFikaGame>.Instance is CoopGame coopGame)
            {
                _botsController.AddActivePLayer(coopGame.LocalPlayer_0);
            }
        }

        var limits = SetMaxBotsLimit(location);
        if (limits > 0)
        {
            _botsController.BotSpawner.SetMaxBots(limits);
        }
    }

    /// <summary>
    /// Made for modders to set custom limits in their mods
    /// </summary>
    /// <param name="location">The map to get the max bots for</param>
    /// <returns>Max bots if any</returns>
    protected int SetMaxBotsLimit(LocationSettingsClass.Location location)
    {
        return 0;
    }

    public async Task StartBotsSystem(LocationSettingsClass.Location location)
    {
#if DEBUG
        Logger.LogWarning("Server: Starting scenarios of bots");
#endif
        if (location.OldSpawn && _wavesSpawnScenario.SpawnWaves != null && _wavesSpawnScenario.SpawnWaves.Length != 0)
        {
            Logger.LogInfo("Running old spawn system. Waves: " + _wavesSpawnScenario.SpawnWaves.Length);
            if (_wavesSpawnScenario != null)
            {
                await _wavesSpawnScenario.Run(EBotsSpawnMode.BeforeGameStarted);
                await _wavesSpawnScenario.Run(EBotsSpawnMode.AfterGameStarted);
            }
        }

        if (location.NewSpawn)
        {
            Logger.LogInfo("Running new spawn system.");
            if (_nonWavesSpawnScenario != null)
            {
                _nonWavesSpawnScenario.Run();
            }
        }

        _bossSpawnScenario.Run(_botsController.BotSpawner.GetPmcZones());
        _botsController.EventsController.SpawnAction();
    }

    public override void SetupEventsAndExfils(Player player)
    {
        if (_fikaGame is not CoopGame coopGame)
        {
            throw new NullReferenceException("Could not find CoopGame");
        }

        coopGame.GameTimer.Start(GameTime, SessionTime);
        coopGame.Spawn();

        SkillClass[] skills = coopGame.Profile_0.Skills.Skills;
        int skillsLength = skills.Length;
        for (int i = 0; i < skillsLength; i++)
        {
            skills[i].SetPointsEarnedInSession(0f, false);
        }

        coopGame.Profile_0.Info.EntryPoint = InfiltrationPoint;
        Logger.LogInfo("[SERVER] SpawnPoint: " + _spawnPoint.Id + ", InfiltrationPoint: " + InfiltrationPoint);

        ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;
        bool isScav = player.Side is EPlayerSide.Savage;
        ExfiltrationPoint[] exfilPoints;
        SecretExfiltrationPoint[] secretExfilPoints;
        exfilController.InitSecretExfils(player);

        if (isScav)
        {
            exfilController.ScavExfiltrationClaim(player.Position, player.ProfileId, player.Profile.FenceInfo.AvailableExitsCount);
            int mask = exfilController.GetScavExfiltrationMask(player.ProfileId);
            exfilPoints = exfilController.ScavExfiltrationClaim(mask, player.ProfileId);
            secretExfilPoints = exfilController.GetScavSecretExits();
        }
        else
        {
            exfilPoints = exfilController.EligiblePoints(coopGame.Profile_0);
            secretExfilPoints = exfilController.SecretEligiblePoints();
        }

        coopGame.GameUi.TimerPanel.SetTime(EFTDateTimeClass.UtcNow,
            coopGame.Profile_0.Info.Side, coopGame.GameTimer.EscapeTimeSeconds(),
            exfilPoints, secretExfilPoints);

        if (TransitControllerAbstractClass.Exist(out FikaHostTransitController transitController))
        {
            transitController.Init();
            // TODO: Sync to clients!!!
        }

        if (Location.EventTrapsData != null)
        {
            Logger.LogInfo("Loading trap data");
            LabyrinthSyncableTrapClass.InitLabyrinthSyncableTraps(Location.EventTrapsData);
            _gameWorld.SyncModule = new();
        }

        ExfilManager.Run(exfilPoints, secretExfilPoints);
        coopGame.Status = GameStatus.Started;
        _botsController.Bots.CheckActivation();

        ConsoleScreen.ApplyStartCommands();
    }

    /// <summary>
    /// When the local player successfully extracts, enable freecam, notify other players about the extract
    /// </summary>
    /// <param name="player">The local player to start the Coroutine on</param>
    /// <param name="exfiltrationPoint">The exfiltration point that was used to extract</param>
    /// <param name="transitPoint">The transit point that was used to transit</param>
    /// <returns></returns>
    public override void Extract(FikaPlayer player, ExfiltrationPoint exfiltrationPoint, TransitPoint transitPoint = null)
    {
        if (_fikaGame is not CoopGame coopGame)
        {
            throw new NullReferenceException("Could not find CoopGame");
        }

        PreloaderUI preloaderUI = Singleton<PreloaderUI>.Instance;
        _localTriggerZones = [.. player.TriggerZones];

        player.ClientMovementContext.SetGravity(false);
        Vector3 position = player.Position;
        position.y += 500;
        player.Teleport(position);

        if (coopGame.ExitStatus == ExitStatus.MissingInAction)
        {
            NotificationManagerClass.DisplayMessageNotification(LocaleUtils.PLAYER_MIA.Localized(), iconType: EFT.Communications.ENotificationIconType.Alert, textColor: Color.red);
        }

        if (player.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController)
        {
            sharedQuestController.ToggleQuestSharing(false);
        }

        BackendConfigSettingsClass.GClass1720.GClass1726 matchEndConfig = Singleton<BackendConfigSettingsClass>.Instance.Experience.MatchEnd;
        if (player.Profile.EftStats.SessionCounters.GetAllInt([CounterTag.Exp]) < matchEndConfig.SurvivedExpRequirement && coopGame.PastTime < matchEndConfig.SurvivedTimeRequirement)
        {
            coopGame.ExitStatus = ExitStatus.Runner;
        }

        if (exfiltrationPoint != null)
        {
            exfiltrationPoint.Disable();

            if (exfiltrationPoint.HasRequirements && exfiltrationPoint.TransferItemRequirement != null)
            {
                if (exfiltrationPoint.TransferItemRequirement.Met(player, exfiltrationPoint) && player.IsYourPlayer)
                {
                    // Seems to already be handled by SPT so we only add it visibly
                    player.Profile.EftStats.SessionCounters.AddDouble(0.2, [CounterTag.FenceStanding, EFenceStandingSource.ExitStanding]);
                }
            }
        }

        if (player.Side == EPlayerSide.Savage)
        {
            // Seems to already be handled by SPT so we only add it visibly
            player.Profile.EftStats.SessionCounters.AddDouble(0.01, [CounterTag.FenceStanding, EFenceStandingSource.ExitStanding]);
        }

        TransitControllerAbstractClass transitController = Singleton<GameWorld>.Instance.TransitController;
        if (transitController != null && transitPoint != null)
        {
            if (transitController.alreadyTransits.TryGetValue(player.ProfileId, out AlreadyTransitDataClass data))
            {
                coopGame.ExitStatus = ExitStatus.Transit;
                coopGame.ExitLocation = transitPoint.parameters.name;
                FikaBackendUtils.IsTransit = true;
            }
            if (transitController is FikaHostTransitController hostController)
            {
                Singleton<IFikaNetworkManager>.Instance.SendGenericPacket(EGenericSubPacketType.UpdateBackendData,
                    UpdateBackendData.FromValue(hostController.AliveTransitPlayers), true);
            }
        }

        if (_coopHandler != null)
        {
            try // This is to allow clients to extract if they lose connection
            {
                Singleton<IFikaNetworkManager>.Instance.SendGenericPacket(EGenericSubPacketType.ClientExtract,
                    ClientExtract.FromValue(player.NetId), true);
                ClearHostAI(player);
            }
            catch
            {

            }

            FikaPlayer fikaPlayer = player;
            coopGame.ExtractedPlayers.Add(fikaPlayer.NetId);
            _coopHandler.ExtractedPlayers.Add(fikaPlayer.NetId);
            _coopHandler.Players.Remove(fikaPlayer.NetId);

            preloaderUI.StartBlackScreenShow(2f, 2f, () =>
            {
                preloaderUI.FadeBlackScreen(2f, -2f);
            });

            player.ActiveHealthController.SetDamageCoeff(0);
            player.ActiveHealthController.DamageMultiplier = 0;
            player.ActiveHealthController.DisableMetabolism();
            player.ActiveHealthController.PauseAllEffects();

            _extractRoutine = coopGame.StartCoroutine(ExtractRoutine(player, coopGame));

            // Prevents players from looting after extracting
            CurrentScreenSingletonClass.Instance.CloseAllScreensForced();

            // Detroys session timer
            if (TimeManager != null)
            {
                UnityEngine.Object.Destroy(TimeManager);
            }
            if (coopGame.GameUi.TimerPanel.enabled)
            {
                coopGame.GameUi.TimerPanel.Close();
            }
        }
        else
        {
            throw new NullReferenceException("Extract: CoopHandler was null!");
        }
    }

    public void ClearHostAI(Player player)
    {
        if (_botsController != null)
        {
            _botsController.DestroyInfo(player);
        }
    }

    public override void CleanUp()
    {
        base.CleanUp();

        if (!FikaBackendUtils.IsHeadless)
        {
            FikaPlayer fikaPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            if (fikaPlayer.PacketSender != null)
            {
                fikaPlayer.PacketSender.DestroyThis();
            }
        }

        if (!FikaBackendUtils.IsTransit)
        {
            NetManagerUtils.StopPinger();
        }
        else
        {
            Singleton<FikaServer>.Instance.HostReady = false;
        }
    }

    public override Task InitializeLoot(LocationSettingsClass.Location location)
    {
        if (FikaPlugin.NoLoot.Value)
        {
            location.Loot = [];
        }

        GClass1947 lootDescriptor = EFTItemSerializerClass.SerializeLootData(location.Loot, FikaGlobals.SearchControllerSerializer);
        EFTWriterClass eftWriter = WriterPoolManager.GetWriter();
        eftWriter.WriteEFTLootDataDescriptor(lootDescriptor);
        LootData = eftWriter.ToArray();
        WriterPoolManager.ReturnWriter(eftWriter);
        return Task.CompletedTask;
    }

    public byte[] GetHostLootItems()
    {
        if (LootData == null || LootData.Length == 0)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            List<LootItemPositionClass> list = new(gameWorld.LootList.Count);
            for (int i = 0; i < gameWorld.LootList.Count; i++)
            {
                IKillableLootItem item = gameWorld.LootList[i];
                if (item is LootItem lootItem && (item is not Corpse or ObservedCorpse))
                {
                    list.Add(SerializeLootItem(lootItem, gameWorld));
                }
            }
            foreach (LootableContainer lootableContainer in LocationScene.GetAllObjects<LootableContainer>(false))
            {
                if (lootableContainer.ItemOwner != null)
                {
                    list.Add(new LootItemPositionClass
                    {
                        Position = lootableContainer.transform.position,
                        Rotation = lootableContainer.transform.rotation.eulerAngles,
                        Item = lootableContainer.ItemOwner.RootItem,
                        ValidProfiles = null,
                        Id = lootableContainer.Id,
                        IsContainer = true
                    });
                }
            }
            foreach (StationaryWeapon stationaryWeapon in LocationScene.GetAllObjects<StationaryWeapon>(false))
            {
                if (!(stationaryWeapon == null) && stationaryWeapon.ItemController != null)
                {
                    list.Add(new LootItemPositionClass
                    {
                        Position = stationaryWeapon.transform.position,
                        Rotation = stationaryWeapon.transform.rotation.eulerAngles,
                        Item = stationaryWeapon.ItemController.RootItem,
                        ValidProfiles = null,
                        Id = stationaryWeapon.Id,
                        IsContainer = true
                    });
                }
            }
            list.Sort(LootCompare);

            GClass1947 lootDescriptor = EFTItemSerializerClass.SerializeLootData(list, FikaGlobals.SearchControllerSerializer);
            EFTWriterClass eftWriter = WriterPoolManager.GetWriter();
            eftWriter.WriteEFTLootDataDescriptor(lootDescriptor);

            byte[] data = eftWriter.ToArray();
            WriterPoolManager.ReturnWriter(eftWriter);
            return data;
        }

        return LootData;
    }

    private LootItemPositionClass SerializeLootItem(LootItem lootItem, GameWorld gameWorld)
    {
        short num = -1;
        if (gameWorld.Platforms.Length != 0 && lootItem.Platform != null)
        {
            num = (short)Array.IndexOf(gameWorld.Platforms, lootItem.Platform);
        }
        /*Corpse corpse;*/
        LootItemPositionClass lootItemPositionClass;
        // TODO: Send corpses instead of killing the players...
        /*if ((corpse = lootItem as Corpse) != null)
        {
            lootItemPositionClass = new GClass1397
            {
                Customization = corpse.Customization,
                Side = corpse.Side,
                Bones = (num > -1) ? corpse.TransformSyncsRelativeToPlatform : corpse.TransformSyncs,
                ProfileID = corpse.PlayerProfileID,
                IsZombieCorpse = corpse.IsZombieCorpse
            };
        }
        else
        {
            lootItemPositionClass = new LootItemPositionClass();
        }*/
        Transform transform = lootItem.transform;
        lootItemPositionClass = new LootItemPositionClass
        {
            Position = (num > -1) ? transform.localPosition : transform.position,
            Rotation = (num > -1) ? transform.localRotation.eulerAngles : transform.rotation.eulerAngles,
            Item = lootItem.ItemOwner.RootItem,
            ValidProfiles = lootItem.ValidProfiles,
            Id = lootItem.StaticId,
            IsContainer = lootItem.StaticId != null,
            Shift = lootItem.Shift,
            PlatformId = num
        };

        return lootItemPositionClass;
    }

    private int LootCompare(LootItemPositionClass a, LootItemPositionClass b)
    {
        return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
    }

    public void StopBotsSystem(bool fromCancel)
    {
        if (!fromCancel)
        {
            GameObject.Destroy(_botStateManager);
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld.ServerShellingController != null)
            {
                UpdateByUnity -= gameWorld.ServerShellingController.OnUpdate;
            }
            _botStateManager.UnassignBotsController();
            _botsController.StopGettingInfo();
            if (!FikaBackendUtils.IsHeadless)
            {
                _botsController.DestroyInfo(_localPlayer);
            }
        }

        _bossSpawnScenario?.Stop();
        if (_nonWavesSpawnScenario != null)
        {
            _nonWavesSpawnScenario.Stop();
        }
        if (_wavesSpawnScenario != null)
        {
            _wavesSpawnScenario.Stop();
        }
    }

    public IEnumerator SyncTraps()
    {
        yield return new WaitForSeconds(5);

        if (Location.EventTrapsData == null)
        {
            yield break;
        }

        if (_gameWorld.SyncModule == null)
        {
            Logger.LogError("SyncModule was null when trying to sync trap data!");
            yield break;
        }

        GClass1368 writer = new(new byte[2048]);
        _gameWorld.SyncModule.Serialize(writer);

        SyncTrapsPacket packet = new()
        {
            Data = new byte[writer.BytesWritten]
        };
        Buffer.BlockCopy(writer.Buffer, 0, packet.Data, 0, writer.BytesWritten);

        Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    private bool IsBarbedWireTrap(TrapSyncable trap)
    {
        return trap.TrapType is ETrapType.BarbedWire;
    }

    private bool IsTrapDoorTrap(TrapSyncable trap)
    {
        return trap.TrapType is ETrapType.TrapDoor;
    }
}
