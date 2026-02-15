using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using CommonAssets.Scripts.Game.LabyrinthEvent;
using EFT;
using EFT.AssetsManager;
using EFT.Bots;
using EFT.Counters;
using EFT.Game.Spawning;
using EFT.GlobalEvents;
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
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Networking.Pooling;
using Fika.Core.UI.Models;
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
        var waves = LocalGame.smethod_7(wavesSettings, location.waves);
        _wavesSpawnScenario = WavesSpawnScenario.smethod_0(abstractGame.gameObject, waves, _botsController.ActivateBotsByWave, location);

        // Boss Scenario setup
        var bossSpawns = LocalGame.smethod_8(true, wavesSettings, location.BossLocationSpawn);
        _bossSpawnScenario = BossSpawnScenario.smethod_0(bossSpawns, _botsController.ActivateBotsByWave);

        _server = Singleton<FikaServer>.Instance;
    }

    public byte[] LootData { get; set; }

    protected readonly BotStateManager _botStateManager;
    protected readonly NonWavesSpawnScenario _nonWavesSpawnScenario;
    protected readonly BotsController _botsController;
    protected readonly WavesSpawnScenario _wavesSpawnScenario;
    protected readonly BossSpawnScenario _bossSpawnScenario;
    protected readonly Dictionary<int, int> _botQueue = [];
    protected readonly FikaServer _server;
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
        var getPlayer = bot.GetPlayer;
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

        _server.HostReady = true;

        var startTime = EFTDateTimeClass.UtcNow.AddSeconds((double)timeBeforeDeployLocal);
        GameTime = startTime;
        _server.GameStartTime = startTime;
        SessionTime = abstractGame.GameTimer.SessionTime;

        InformationPacket packet = new()
        {
            RaidStarted = RaidStarted,
            ReadyPlayers = _server.ReadyClients,
            HostReady = _server.HostReady,
            GameTime = GameTime.Value,
            SessionTime = SessionTime.Value,
            GameDateTime = GameDateTime
        };

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
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

        var netId = 1000;
        FikaBot fikaBot;
        if (Bots.ContainsKey(profile.Id))
        {
            return null;
        }

        netId = _server.PopNetId();

        profile.SetSpawnedInSession(profile.Info.Side == EPlayerSide.Savage || FikaPlugin.Instance.PMCFoundInRaid);

        var mongoId = MongoID.Generate(true);
        const ushort nextOperationId = 0;
        var packet = SendCharacterPacket.FromValue(new()
        {
            Profile = profile,
            ControllerId = mongoId,
            FirstOperationId = nextOperationId,
            IsZombie = profile.Info.Settings.UseSimpleAnimator
        }, true, true, position, netId);
        packet.PlayerInfoPacket.HealthByteArray = profile.Health.SerializeHealthInfo();
        _server.SendGenericPacket(EGenericSubPacketType.SendCharacter, packet, true);

        if (_server.NetServer.ConnectedPeersCount > 0)
        {
            await WaitForPlayersToLoadBotProfile(netId);
        }

        fikaBot = await FikaBot.CreateBot(_gameWorld, netId, position, Quaternion.identity, "Player",
           "Bot_", EPointOfView.ThirdPerson, profile, true, _updateQueue, Player.EUpdateMode.Auto,
           Player.EUpdateMode.Auto, BackendConfigAbstractClass.Config.CharacterController.BotPlayerMode,
           FikaGlobals.GetOtherPlayerSensitivity, FikaGlobals.GetOtherPlayerSensitivity,
           ObservedViewFilter.Default, mongoId, nextOperationId);

        fikaBot.Location = Location.Id;
        Bots.Add(fikaBot.ProfileId, fikaBot);

        if (FikaPlugin.Instance.Settings.DisableBotMetabolism.Value)
        {
            fikaBot.HealthController.DisableMetabolism();
        }
        _coopHandler.Players.Add(fikaBot.NetId, fikaBot);

        if (profile.Info.Settings.Role != WildSpawnType.shooterBTR)
        {
            _botStateManager.AddBot(fikaBot);
        }

        var spawnPacket = SpawnAI.FromValue(netId, position);
        _server.SendGenericPacket(EGenericSubPacketType.SpawnAI, spawnPacket);

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
        var connectedPeers = _server.NetServer.ConnectedPeersCount;

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
            connectedPeers = _server.NetServer.ConnectedPeersCount;
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
        var botOwner = bot.AIData.BotOwner;

        _botsController.Bots.Remove(botOwner);
        bot.HealthController.DiedEvent -= botOwner.method_6; // Unsubscribe from the event to prevent errors.
        BotDespawn(botOwner);
        if (botOwner != null)
        {
            botOwner.Dispose();
        }

        var fikaPlayer = (FikaPlayer)bot;
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

        var dateTime = EFTDateTimeClass.StartOfDay();
        var dateTime2 = dateTime.AddDays(1);

        var weather = WeatherClass.CreateDefault();
        var weather2 = WeatherClass.CreateDefault();
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
        LoadingScreenUI.Instance.UpdateAndBroadcast(80f);

        if (Location.Id == "laboratory")
        {
            Logger.LogInfo("Location is 'Laboratory', skipping weather generation");
            Season = ESeason.Summer;
            FikaBackendUtils.CustomRaidSettings.UseCustomWeather = false;
        }
        else
        {
            await GenerateWeathers();
        }

        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_FINISHING_RAID_INIT.Localized());

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
            var freeCamController = gameWorld.gameObject.AddComponent<FreeCameraController>();
            Singleton<FreeCameraController>.Create(freeCamController);
        }

        await SetupRaidCode();

        LoadingScreenUI.Instance.UpdateAndBroadcast(85f);

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

        if (FikaPlugin.Instance.Settings.DevMode.Value)
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

        if (FikaPlugin.Instance.Settings.UseNATPunching.Value)
        {
            _server.StopNatIntroduceRoutine();
        }

        if (startButton != null)
        {
            GameObject.Destroy(startButton);
        }

        InformationPacket continuePacket = new()
        {
            AmountOfPeers = _server.NetServer.ConnectedPeersCount + 1
        };
        _server.SendData(ref continuePacket, DeliveryMethod.ReliableOrdered);
        SetStatusModel status = new(FikaBackendUtils.GroupId, LobbyEntry.ELobbyStatus.IN_GAME);
        await FikaRequestHandler.UpdateSetStatus(status);
    }

    public override async Task WaitForOtherPlayersToLoad()
    {
        float expectedPlayers = _server.PlayerAmount;
        if (expectedPlayers <= 1)
        {
            _server.ReadyClients++;
            return;
        }

        if (FikaBackendUtils.IsHeadless)
        {
            expectedPlayers--;
        }
#if DEBUG
        Logger.LogWarning("Server: Waiting for coopHandler.AmountOfHumans < expected players, expected: " + expectedPlayers);
#endif
        _server.ReadyClients++;
        do
        {
            await Task.Delay(100);
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized(), (float)_coopHandler.AmountOfHumans / expectedPlayers);
        } while (_coopHandler.AmountOfHumans < expectedPlayers);

        InformationPacket packet = new()
        {
            RaidStarted = RaidStarted,
            ReadyPlayers = _server.ReadyClients
        };

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);

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
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized(), (float)_server.ReadyClients / expectedPlayers);
        } while (_server.ReadyClients < expectedPlayers);

        InformationPacket finalPacket = new()
        {
            RaidStarted = RaidStarted,
            ReadyPlayers = _server.ReadyClients
        };

        _server.SendData(ref finalPacket, DeliveryMethod.ReliableOrdered);
    }

    public override async Task GenerateWeathers()
    {
        if (WeatherController.Instance != null)
        {
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_INIT_WEATHER.Localized());
            Logger.LogInfo("Generating and initializing weather...");
            var weather = await _backendSession.WeatherRequest();
            Season = _backendSession.Season;
            SeasonsSettings = _backendSession.SeasonsSettings;
            if (!FikaBackendUtils.CustomRaidSettings.UseCustomWeather)
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
        var spawnSafeDistance = (Location.SpawnSafeDistanceMeters > 0) ? Location.SpawnSafeDistanceMeters : 100;
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
        var halloween = location.Events.Halloween2024;
        if (halloween?.InfectionPercentage > 0)
        {
            waveInfos.AddRange(BotHalloweenWithZombies.GetProfilesOnStart());
        }
        await botsPresets.TryLoadBotsProfilesOnStart(waveInfos);
        BotCreatorClass botCreator = new(this, botsPresets, CreateBot);
        BotZone[] botZones = [.. LocationScene.GetAllObjects<BotZone>(false)];

        var useWaveControl = controllerSettings.BotAmount == EBotAmount.Horde;

        var limits = SetMaxBotsLimit(location);
        if (FikaPlugin.Instance.Settings.NoAI.Value)
        {
            FikaGlobals.LogWarning("No AI enabled - stopping bot spawns");
            controllerSettings.BotAmount = EBotAmount.NoBots;
            limits = 0;
        }

        var numberOfBots = controllerSettings.BotAmount switch
        {
            EBotAmount.AsOnline => _backendSession.BackEndConfig.Config.MaxBotsAliveOnMap,
            EBotAmount.NoBots => 0,
            EBotAmount.Low => 15,
            EBotAmount.Medium => 20,
            EBotAmount.High => 25,
            EBotAmount.Horde => 35,
            _ => 15,
        };

        _botsController.SetSettings(limits > 0 ? limits : numberOfBots, _backendSession.BackEndConfig.BotPresets, _backendSession.BackEndConfig.BotWeaponScatterings);
        _botsController.Init(this, botCreator, botZones, SpawnSystem, _wavesSpawnScenario.BotLocationModifier,
            controllerSettings.IsEnabled, controllerSettings.IsScavWars, useWaveControl, false,
            _bossSpawnScenario.HaveSectants, gameWorld, location.OpenZones, location.Events);
        UpdateByUnity -= _botsController.method_0;
        if (controllerSettings.ExcludedBosses != null)
        {
            _botsController.BotSpawner.SetBlockedRoles(controllerSettings.ExcludedBosses);
        }
        _botStateManager.AssignBotsController(_botsController);

        if (!FikaBackendUtils.IsHeadless && Singleton<IFikaGame>.Instance is CoopGame coopGame)
        {
            _botsController.AddActivePLayer(coopGame.LocalPlayer_0);
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

        var skills = coopGame.Profile_0.Skills.Skills;
        var skillsLength = skills.Length;
        for (var i = 0; i < skillsLength; i++)
        {
            skills[i].SetPointsEarnedInSession(0f, false);
        }

        coopGame.Profile_0.Info.EntryPoint = InfiltrationPoint;
        Logger.LogInfo("[SERVER] SpawnPoint: " + _spawnPoint.Id + ", InfiltrationPoint: " + InfiltrationPoint);

        var exfilController = ExfiltrationControllerClass.Instance;
        var isScav = player.Side is EPlayerSide.Savage;
        ExfiltrationPoint[] exfilPoints;
        SecretExfiltrationPoint[] secretExfilPoints;
        exfilController.InitSecretExfils(player);

        if (isScav)
        {
            exfilController.ScavExfiltrationClaim(player.Position, player.ProfileId, player.Profile.FenceInfo.AvailableExitsCount);
            var mask = exfilController.GetScavExfiltrationMask(player.ProfileId);
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
            foreach (var activePlayer in CoopHandler.HumanPlayers)
            {
                var initEvent = new TransitInitEvent
                {
                    PlayerId = activePlayer.Id,
                    Points = Location.transitParameters.Where(x => x.active).ToDictionary(k => k.id),
                    TransitionCount = (ushort)transitController.LocalRaidSettings_0.transition.transitionCount,
                    EventPlayer = transitController.IsEvent
                };

                var writer = NetworkUtils.EventDataWriter;
                writer.Reset();
                initEvent.Serialize(ref writer);
                writer.Flush();

                var syncPacket = new SyncEventPacket
                {
                    Type = 0,
                    Data = new byte[writer.BytesWritten]
                };
                Array.Copy(writer.Buffer, syncPacket.Data, writer.BytesWritten);
                _server.SendData(ref syncPacket, DeliveryMethod.ReliableOrdered);

                var updateEvent = new TransitUpdateEvent
                {
                    PlayerId = activePlayer.Id,
                    EventOnly = transitController.IsEvent,
                    Points = Location.transitParameters.Where(x => x.active).ToDictionary(k => k.id)
                };

                writer.Reset();
                updateEvent.Serialize(ref writer);
                writer.Flush();

                syncPacket.Type = 1;
                Array.Copy(writer.Buffer, syncPacket.Data, writer.BytesWritten);
                _server.SendData(ref syncPacket, DeliveryMethod.ReliableOrdered);
            }
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

        var preloaderUI = Singleton<PreloaderUI>.Instance;
        _localTriggerZones = [.. player.TriggerZones];

        player.ClientMovementContext.SetGravity(false);
        var position = player.Position;
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

        var matchEndConfig = Singleton<BackendConfigSettingsClass>.Instance.Experience.MatchEnd;
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

        var transitController = Singleton<GameWorld>.Instance.TransitController;
        if (transitController != null && transitPoint != null)
        {
            if (transitController.alreadyTransits.TryGetValue(player.ProfileId, out var data))
            {
                coopGame.ExitStatus = ExitStatus.Transit;
                coopGame.ExitLocation = transitPoint.parameters.name;
                FikaBackendUtils.IsTransit = true;
            }
            if (transitController is FikaHostTransitController hostController)
            {
                _server.SendGenericPacket(EGenericSubPacketType.UpdateBackendData,
                    UpdateBackendData.FromValue(hostController.AliveTransitPlayers), true);
            }
        }

        if (_coopHandler != null)
        {
            try // This is to allow clients to extract if they lose connection
            {
                _server.SendGenericPacket(EGenericSubPacketType.ClientExtract,
                    ClientExtract.FromValue(player.NetId), true);
                ClearHostAI(player);
            }
            catch
            {

            }

            var fikaPlayer = player;
            coopGame.ExtractedPlayers.Add(fikaPlayer.NetId);
            _coopHandler.ExtractedPlayers.Add(fikaPlayer.NetId);
            _coopHandler.Players.Remove(fikaPlayer.NetId);

            preloaderUI.StartBlackScreenShow(2f, 2f, () => preloaderUI.FadeBlackScreen(2f, -2f));

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
            var fikaPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
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
            _server.HostReady = false;
        }

        _btrSpawn?.Invoke();
        _btrSpawn = null;
    }

    public override Task InitializeLoot(LocationSettingsClass.Location location)
    {
        if (FikaPlugin.Instance.Settings.NoLoot.Value)
        {
            location.Loot = [];
        }

        var lootDescriptor = EFTItemSerializerClass.SerializeLootData(location.Loot, FikaGlobals.SearchControllerSerializer);
        var eftWriter = WriterPoolManager.GetWriter();
        eftWriter.WriteEFTLootDataDescriptor(lootDescriptor);
        LootData = eftWriter.ToArray();
        WriterPoolManager.ReturnWriter(eftWriter);
        return Task.CompletedTask;
    }

    public byte[] GetHostLootItems()
    {
        if (LootData == null || LootData.Length == 0)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            List<LootItemPositionClass> list = new(gameWorld.LootList.Count);
            for (var i = 0; i < gameWorld.LootList.Count; i++)
            {
                var item = gameWorld.LootList[i];
                if (item is LootItem lootItem && (item is not Corpse or ObservedCorpse))
                {
                    list.Add(SerializeLootItem(lootItem, gameWorld));
                }
            }
            foreach (var lootableContainer in LocationScene.GetAllObjects<LootableContainer>(false))
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
            foreach (var stationaryWeapon in LocationScene.GetAllObjects<StationaryWeapon>(false))
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

            var lootDescriptor = EFTItemSerializerClass.SerializeLootData(list, FikaGlobals.SearchControllerSerializer);
            var eftWriter = WriterPoolManager.GetWriter();
            eftWriter.WriteEFTLootDataDescriptor(lootDescriptor);

            var data = eftWriter.ToArray();
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
        //LootItemPositionClass lootItemPositionClass;
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
        var transform = lootItem.transform;
        return new LootItemPositionClass
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
    }

    private int LootCompare(LootItemPositionClass a, LootItemPositionClass b)
    {
        return string.CompareOrdinal(a.Id, b.Id);
    }

    public void StopBotsSystem(bool fromCancel)
    {
        if (!fromCancel)
        {
            GameObject.Destroy(_botStateManager);
            var gameWorld = Singleton<GameWorld>.Instance;
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

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
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
