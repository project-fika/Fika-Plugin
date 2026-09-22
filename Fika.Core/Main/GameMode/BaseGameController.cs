using CommonAssets.Scripts.ArtilleryShelling.Client;
using EFT.InventoryLogic;
using EFT.Vehicle;
using EFT.Weather;
using JsonType;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audio.AmbientSubsystem;
using BepInEx.Logging;
using Comfort.Common;
using CommonAssets.Scripts.Audio.RadioSystem;
using Dissonance;
using EFT;
using EFT.Bots;
using EFT.Game.Spawning;
using EFT.GlobalEvents;
using EFT.Interactive;
using EFT.UI;
using EFT.UI.Matchmaker;
using EFT.UI.Screens;
using Fika.Core.Bundles;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.Components;
using Fika.Core.Main.HostClasses;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.World;
using HarmonyLib;
using UnityEngine.Events;
using static JsonType.LocationSettings;
using ClientTransitController = Fika.Core.Main.ClientClasses.ClientTransitController;
using ClientRunddansController = Fika.Core.Main.ClientClasses.ClientRunddansController;

namespace Fika.Core.Main.GameMode;

/// <summary>
/// Abstract base controller managing core raid lifecycle, networking synchronization,
/// environment/weather configuration, and game world systems for Fika co-op sessions.
/// </summary>
public abstract class BaseGameController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseGameController"/> class.
    /// </summary>
    /// <param name="game">The Fika game instance.</param>
    /// <param name="updateQueue">The update queue used for tick processing.</param>
    /// <param name="gameWorld">The EFT game world instance.</param>
    /// <param name="session">The backend EFT session.</param>
    public BaseGameController(IFikaGame game, EUpdateQueue updateQueue, GameWorld gameWorld, IEftSession session)
    {
        _fikaGame = game;
        _abstractGame = (AbstractGame)game;
        _updateQueue = updateQueue;
        _gameWorld = gameWorld;
        _backendSession = session;
        IsServer = FikaBackendUtils.IsServer;

        Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
    }

    /// <summary>
    /// Gets or sets the logger instance for this controller.
    /// </summary>
    public ManualLogSource Logger { get; set; }

    /// <summary>
    /// Gets the underlying EFT <see cref="AbstractGame"/> instance.
    /// </summary>
    public AbstractGame GameInstance
    {
        get
        {
            return _abstractGame;
        }
    }

    /// <summary>
    /// The active Fika game interface instance.
    /// </summary>
    protected IFikaGame _fikaGame;

    /// <summary>
    /// The underlying EFT <see cref="AbstractGame"/> instance.
    /// </summary>
    protected AbstractGame _abstractGame;

    /// <summary>
    /// Gets a value indicating whether this controller is executing as the host/server.
    /// </summary>
    public bool IsServer { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the raid has started.
    /// </summary>
    public bool RaidStarted { get; internal set; }

    /// <summary>
    /// Gets or sets the time manager handling in-raid time synchronization.
    /// </summary>
    public FikaTimeManager TimeManager { get; set; }

    // Weather

    /// <summary>
    /// Gets or sets the current season applied to the raid environment.
    /// </summary>
    public ESeason Season
    {
        get
        {
            return _season;
        }
        set
        {
            _season = value;
            Logger.LogInfo($"Setting Season to: {value}");
            WeatherReady = true;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether weather data has been loaded and initialized.
    /// </summary>
    public bool WeatherReady { get; internal set; }

    /// <summary>
    /// Gets or sets the generated weather nodes representing weather states over time.
    /// </summary>
    public WeatherNode[] WeatherClasses { get; set; }

    /// <summary>
    /// Gets or sets season-specific configuration settings.
    /// </summary>
    public SeasonsSettings SeasonsSettings { get; set; }

    /// <summary>
    /// Gets or sets the exfiltration manager handling extraction points.
    /// </summary>
    public FikaExfilManager ExfilManager { get; set; }

    // Raid data

    /// <summary>
    /// Gets or sets the collection of active thrown grenades in the world.
    /// </summary>
    public List<ThrowWeap> ThrownGrenades { get; set; }

    /// <summary>
    /// Gets or sets the current raid configuration settings.
    /// </summary>
    public RaidSettings RaidSettings { get; set; }

    /// <summary>
    /// Gets or sets the synchronized loot data for the current raid.
    /// </summary>
    public LootData LootItems { get; set; } = [];

    /// <summary>
    /// Gets or sets the location settings definition for the current raid map.
    /// </summary>
    public LocationSettings.Location Location { get; set; }

    /// <summary>
    /// Active AI bots in the raid mapped by their ID.
    /// </summary>
    public Dictionary<string, Player> Bots = [];

    /// <summary>
    /// Gets the co-op component handler responsible for player synchronization.
    /// </summary>
    public CoopHandler CoopHandler
    {
        get
        {
            return _coopHandler;
        }
    }

    /// <summary>
    /// Gets or sets the in-game DateTime for the raid.
    /// </summary>
    public DateTime? GameTime { get; set; }

    /// <summary>
    /// Gets or sets the session duration for the raid.
    /// </summary>
    public TimeSpan? SessionTime { get; set; }

    /// <summary>
    /// Gets the list of active local trigger zone identifiers.
    /// </summary>
    public List<string> LocalTriggerZones
    {
        get
        {
            return _localTriggerZones;
        }
    }

    /// <summary>
    /// Active local trigger zone identifiers.
    /// </summary>
    protected List<string> _localTriggerZones = [];

    // Spawns

    /// <summary>
    /// Gets or sets the assigned spawn position for the client.
    /// </summary>
    public Vector3 ClientSpawnPosition { get; set; }

    /// <summary>
    /// Gets or sets the assigned spawn rotation for the client.
    /// </summary>
    public Quaternion ClientSpawnRotation { get; set; }

    /// <summary>
    /// Gets or sets the spawn system responsible for spawning entities.
    /// </summary>
    public ISpawnSystem SpawnSystem { get; set; }

    /// <summary>
    /// Gets or sets the chosen infiltration entry point name.
    /// </summary>
    public string InfiltrationPoint { get; set; }

    /// <summary>
    /// Gets the designated player spawn point.
    /// </summary>
    public ISpawnPoint SpawnPoint
    {
        get
        {
            return _spawnPoint;
        }
    }

    private FikaHalloweenEventManager _halloweenEventManager;
    private DebugUI _debugUi;
    private ESeason _season;

    /// <summary>
    /// Collection of all available spawn points for the location.
    /// </summary>
    protected SpawnPointsCollection _spawnPoints;

    /// <summary>
    /// The designated spawn point assigned to the player.
    /// </summary>
    protected ISpawnPoint _spawnPoint;

    /// <summary>
    /// Unsubscription delegate for BTR spawn events.
    /// </summary>
    protected Action _btrSpawn;

    /// <summary>
    /// The co-op handler component.
    /// </summary>
    protected CoopHandler _coopHandler;

    /// <summary>
    /// The local player instance.
    /// </summary>
    protected FikaPlayer _localPlayer;

    /// <summary>
    /// The game update queue used for tick processing.
    /// </summary>
    protected EUpdateQueue _updateQueue;

    /// <summary>
    /// The current EFT game world instance.
    /// </summary>
    protected GameWorld _gameWorld;

    /// <summary>
    /// The backend EFT session.
    /// </summary>
    protected IEftSession _backendSession;

    /// <summary>
    /// Active coroutine reference for extraction damage prevention.
    /// </summary>
    protected Coroutine _extractRoutine;

    /// <summary>
    /// Sets the local player instance and assigns it to the co-op handler.
    /// </summary>
    /// <param name="player">The local player instance.</param>
    public void SetLocalPlayer(FikaPlayer player)
    {
        _localPlayer = player;
        _coopHandler.MyPlayer = player;
    }

    /// <summary>
    /// <see cref="Task"/> used to wait for host to start the raid
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous wait operation.</returns>
    public virtual Task WaitForHostToStart()
    {
        Logger.LogInfo("Starting task to wait for host to start the raid.");
        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_HOST_START_RAID.Localized());

        return Task.CompletedTask;
    }

    /// <summary>
    /// This creates a "custom" Back button so that we can back out if we get stuck
    /// </summary>
    /// <returns>The created custom start button <see cref="GameObject"/>, or <see langword="null"/> if the menu UI is not instantiated.</returns>
    protected GameObject CreateStartButton()
    {
        if (MenuUI.Instantiated)
        {
            var menuUI = MenuUI.Instance;
            var backButton = Traverse.Create(menuUI.MatchmakerTimeHasCome).Field<DefaultUIButton>("_cancelButton").Value;
            var customButton = GameObject.Instantiate(backButton.gameObject, backButton.gameObject.transform.parent);
            customButton.gameObject.name = "FikaStartButton";
            customButton.gameObject.SetActive(true);
            var backButtonComponent = customButton.GetComponent<DefaultUIButton>();
            backButtonComponent.SetHeaderText(LocaleUtils.UI_START_RAID.Localized(), 32);
            backButtonComponent.SetEnabledTooltip(LocaleUtils.UI_START_RAID_DESCRIPTION.Localized());
            UnityEvent newEvent = new();
            newEvent.AddListener(() =>
            {
                if (IsServer)
                {
                    RaidStarted = true;
                    return;
                }

                var fikaClient = Singleton<FikaClient>.Instance ?? throw new NullReferenceException("CreateStartButton::FikaClient was null!");
                InformationPacket packet = new()
                {
                    RequestStart = true
                };
                fikaClient.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            });
            Traverse.Create(backButtonComponent).Field("OnClick").SetValue(newEvent);

            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.QuestStarted);

            return customButton;
        }

        return null;
    }

    /// <summary>
    /// Waits for host initialization before allowing local player deployment.
    /// </summary>
    /// <param name="timeBeforeDeployLocal">The countdown duration in seconds before local deployment.</param>
    /// <returns>An enumerator for coroutine progression.</returns>
    public abstract IEnumerator WaitForHostInit(int timeBeforeDeployLocal);

    /// <summary>
    /// Gets the appropriate player spawn position depending on whether this instance is server or client.
    /// </summary>
    /// <returns>The spawn position vector.</returns>
    public Vector3 GetSpawnPosition()
    {
        return IsServer ? _spawnPoint.Position : ClientSpawnPosition;
    }

    /// <summary>
    /// Gets the appropriate player spawn rotation depending on whether this instance is server or client.
    /// </summary>
    /// <returns>The spawn rotation quaternion.</returns>
    public Quaternion GetSpawnRotation()
    {
        return IsServer ? _spawnPoint.Rotation : ClientSpawnRotation;
    }

    /// <summary>
    /// Instantiates and initializes the Fika debug UI component.
    /// </summary>
    public virtual void CreateDebugComponent()
    {
        var asset = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.DebugUI);
        var debugObject = GameObject.Instantiate(asset);
        _debugUi = debugObject.GetComponent<DebugUI>();
        debugObject.SetActive(false);
    }

    /// <summary>
    /// Initializes and attaches the co-op handler component to the controller and game instance.
    /// </summary>
    /// <param name="fikaGame">The Fika game instance.</param>
    /// <returns>A completed <see cref="Task"/> upon successful setup.</returns>
    /// <exception cref="NullReferenceException">Thrown when the co-op handler cannot be found.</exception>
    public Task SetupCoopHandler(IFikaGame fikaGame)
    {
        if (CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            _coopHandler = coopHandler;
            _coopHandler.LocalGameInstance = fikaGame;
            if (IsServer && FikaBackendUtils.IsTransit)
            {
                coopHandler.ReInitInteractables();
            }

            return Task.CompletedTask;
        }
        else
        {
            throw new NullReferenceException("CoopHandler was missing!");
        }
    }

    /// <summary>
    /// Coroutine that waits for transit and BTR controllers to initialize, then initializes player transfer stashes and synchronizes them.
    /// </summary>
    /// <returns>An enumerator for coroutine progression.</returns>
    public IEnumerator CreateStashes()
    {
        WaitForSeconds waitForSeconds = new(0.5f);
        if (_gameWorld.TransitController != null)
        {
            while (_gameWorld.TransitController.TransferItemsController == null)
            {
                yield return waitForSeconds;
            }

            while (_gameWorld.TransitController.TransferItemsController.Stash == null)
            {
                yield return waitForSeconds;
            }
        }

        if (_gameWorld.BtrController != null)
        {
            while (_gameWorld.BtrController.TransferItemsController == null)
            {
                yield return waitForSeconds;
            }

            while (_gameWorld.BtrController.TransferItemsController.Stash == null)
            {
                yield return waitForSeconds;
            }
        }

        if (_coopHandler != null)
        {
            for (var i = 0; i < _coopHandler.HumanPlayers.Count; i++)
            {
                var player = _coopHandler.HumanPlayers[i];
                try
                {
                    if (_gameWorld.BtrController != null)
                    {
                        _gameWorld.BtrController.TransferItemsController.InitPlayerStash(player);
                    }

                    if (_gameWorld.TransitController != null)
                    {
                        _gameWorld.TransitController.TransferItemsController.InitPlayerStash(player);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Could not initialize transfer stash on {player.Profile.Nickname}: {ex.Message}");
                }
            }

            Singleton<FikaServer>.Instance.SendStashes();
        }
        else
        {
            Logger.LogError("Could not find CoopHandler when trying to initialize player stashes for TransferItemsController!");
        }
    }

    /// <summary>
    /// This task ensures that all players are joined and loaded before continuing
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous wait operation.</returns>
    public abstract Task WaitForOtherPlayersToLoad();

    /// <summary>
    /// Runs a few last changes to the raid setup
    /// </summary>
    /// <returns>An enumerator for coroutine progression.</returns>
    public virtual IEnumerator FinishRaidSetup()
    {
        LoadingScreenUI.Instance.UpdateAndBroadcast(90f);

        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_FINISHING_RAID_INIT.Localized());

        WaitForEndOfFrame endOfFrame = new();
        var musicTask = Singleton<GUISounds>.Instance.FadeBackgroundMusicAsync(false, CancellationToken.None);
        while (!musicTask.IsCompleted)
        {
            yield return endOfFrame;
        }

        AudioUtils.ResetAudioBuffer();

        _gameWorld.TriggersModule = _abstractGame.gameObject.AddComponent<LocalFikaTriggersModule>();
        _gameWorld.FillLampControllers();
        if (Location.Id == "laboratory")
        {
            Season = ESeason.Summer;
        }
        WeatherReady = true;
        FikaBackendUtils.CustomRaidSettings.UseCustomWeather = false;

        SeasonsController seasonController = new();
        _gameWorld.SeasonsController = seasonController;

        LoadingScreenUI.Instance.UpdateAndBroadcast(100f);

#if DEBUG
        Logger.LogWarning($"Running season handler for season: {Season}");
#endif
        var runSeason = seasonController.Run(Season, SeasonsSettings);
        while (!runSeason.IsCompleted)
        {
            yield return endOfFrame;
        }

        if (MonoBehaviourSingleton<RadioBroadcastController>.Instantiated)
        {
            MonoBehaviourSingleton<RadioBroadcastController>.Instance.StartBroadcast();
        }
    }

    /// <summary>
    /// Generates or synchronizes weather conditions for the current raid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public abstract Task GenerateWeathers();

    /// <summary>
    /// Displays the final countdown screen, initializes ambient audio, synchronizes transit parameters, and dispatches the raid start event.
    /// </summary>
    /// <param name="profile">The player profile.</param>
    /// <param name="profileId">The player profile ID.</param>
    /// <returns>An enumerator for coroutine progression.</returns>
    public virtual IEnumerator CountdownScreen(Profile profile, string profileId)
    {
        FikaBackendUtils.GroupPlayers.Clear();

        var timeBeforeDeployLocal = FikaBackendUtils.IsReconnect ? 3 : Singleton<GlobalConfiguration>.Instance.TimeBeforeDeployLocal;
#if DEBUG
        timeBeforeDeployLocal = 3;
#endif
        yield return WaitForHostInit(timeBeforeDeployLocal);

        NetManagerUtils.DisableLoadingScreenUI();

        var dateTime = DateTimeExtensions.Now.AddSeconds(timeBeforeDeployLocal);
        new MatchmakerFinalCountdown.FinalCountdownScreenController(profile, dateTime).ShowScreen(EScreenState.Root);
        if (MonoBehaviourSingleton<AmbientAudioSystem>.Instantiated)
        {
            MonoBehaviourSingleton<AmbientAudioSystem>.Instance.Initialize();
        }
        MonoBehaviourSingleton<BetterAudio>.Instance.FadeInVolumeBeforeRaid(timeBeforeDeployLocal);
        Singleton<GUISounds>.Instance.method_9(false);
        Singleton<GUISounds>.Instance.StopMenuBackgroundMusicWithDelay(timeBeforeDeployLocal);
        _abstractGame.GameUi.gameObject.SetActive(true);
        _abstractGame.GameUi.TimerPanel.ProfileId = profileId;
        yield return new WaitForSeconds(timeBeforeDeployLocal);
        SyncTransitControllers(profileId);
        FikaEventDispatcher.DispatchEvent(new FikaRaidStartedEvent(FikaBackendUtils.IsServer));

        if (Singleton<IFikaNetworkManager>.Instance.AllowVOIP && !FikaBackendUtils.IsHeadless)
        {
            _abstractGame.StartCoroutine(FixVOIPAudioDevice());
        }
    }

    /// <summary>
    /// Coroutine workaround that cycles VoIP audio sources to ensure peer VoIP playback works correctly.
    /// </summary>
    /// <returns>An enumerator for coroutine progression.</returns>
    private IEnumerator FixVOIPAudioDevice()
    {
        // Todo: Find root causes and fix elegantly...
        DissonanceComms.Instance.IsMuted = false;
        yield return new WaitForSeconds(1);
        DissonanceComms.Instance.IsMuted = true;

        for (var i = 0; i < _coopHandler.HumanPlayers.Count; i++)
        {
            var player = _coopHandler.HumanPlayers[i];
            if (player.IsYourPlayer)
            {
                continue;
            }
            if (player.VoipAudioSource == null)
            {
                Logger.LogError($"FixVOIPAudioDevice: VoipAudioSource was null for {player.Profile.Nickname}");
                continue;
            }
            player.VoipAudioSource.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(2);

        for (var i = 0; i < _coopHandler.HumanPlayers.Count; i++)
        {
            var player = _coopHandler.HumanPlayers[i];
            if (player.IsYourPlayer)
            {
                continue;
            }
            if (player.VoipAudioSource == null)
            {
                Logger.LogError($"FixVOIPAudioDevice: VoipAudioSource was null for {player.Profile.Nickname}");
                continue;
            }
            player.VoipAudioSource.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Sends summoned transit configuration and parameters for the specified profile over the network.
    /// </summary>
    /// <param name="profileId">The profile ID whose transit information is synchronized.</param>
    private void SyncTransitControllers(string profileId)
    {
        var transitController = Singleton<GameWorld>.Instance.TransitController;
        if (transitController == null)
        {
            if (FikaPlugin.Instance.Settings.EnableTransits)
            {
                Logger.LogError("SyncTransitControllers: TransitController was null!");
            }
            return;
        }

        if (transitController.summonedTransits.TryGetValue(profileId, out var transitData))
        {
            SyncTransitControllersPacket packet = new()
            {
                ProfileId = profileId,
                RaidId = transitData.raidId,
                Count = transitData.count,
                Maps = transitData.maps,
                Events = transitData.events
            };

            Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            return;
        }

        Logger.LogError("SyncTransitControllers: Could not find TransitData in Summonedtransits!");
    }

    /// <summary>
    /// Asynchronously assigns or receives the designated spawn point for the specified player profile.
    /// </summary>
    /// <param name="profile">The player profile.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public abstract Task ReceiveSpawnPoint(Profile profile);

    /// <summary>
    /// Creates and initializes the spawn system.
    /// </summary>
    /// <param name="profile">The player profile.</param>
    public abstract void CreateSpawnSystem(Profile profile);

    /// <summary>
    /// Initializes the server and client artillery shelling controllers if configured for the current location.
    /// </summary>
    /// <param name="instance">The global configuration instance.</param>
    /// <param name="gameWorld">The game world instance.</param>
    /// <param name="location">The current raid location definition.</param>
    public void InitShellingController(GlobalConfiguration instance, GameWorld gameWorld, LocationSettings.Location location)
    {
        if (instance != null && instance.ArtilleryShelling != null && instance.ArtilleryShelling.ArtilleryMapsConfigs?.Keys.Contains(location.Id) == true)
        {
            if (IsServer)
            {
                gameWorld.ServerShellingController = new ArtilleryShellingControllerServer();
            }
            gameWorld.ClientShellingController = new ArtilleryShellingControllerClient(IsServer);
        }
    }

    /// <summary>
    /// Initializes Halloween event prefabs and controllers if the event is active for the current location.
    /// </summary>
    /// <param name="instance">The global configuration instance.</param>
    /// <param name="gameWorld">The game world instance.</param>
    /// <param name="location">The current raid location definition.</param>
    public void InitHalloweenEvent(GlobalConfiguration instance, GameWorld gameWorld, LocationSettings.Location location)
    {
        if (instance != null && instance.EventSettings.EventActive && !instance.EventSettings.LocationsToIgnore.Contains(location.Id))
        {
#if DEBUG
            Logger.LogWarning("Spawning halloween prefabs");
#endif
            gameWorld.HalloweenEventController = new HalloweenEventController();
            var gameObject = (GameObject)Resources.Load("Prefabs/HALLOWEEN_CONTROLLER");
            if (gameObject != null)
            {
                _abstractGame.transform.InstantiatePrefab(gameObject);
            }
            else
            {
                Logger.LogError("InitHalloweenEvent: Error loading Halloween assets!");
            }

            if (IsServer)
            {
                _halloweenEventManager = gameWorld.gameObject.GetOrAddComponent<FikaHalloweenEventManager>();
            }
        }
    }

    /// <summary>
    /// Initializes the BTR vehicle controller and event subscription if BTR is enabled on the current location.
    /// </summary>
    /// <param name="instance">The global configuration instance.</param>
    /// <param name="gameWorld">The game world instance.</param>
    /// <param name="location">The current raid location definition.</param>
    public void InitBTRController(GlobalConfiguration instance, GameWorld gameWorld, LocationSettings.Location location)
    {
        if (FikaPlugin.Instance.Settings.UseBTR)
        {
            if (instance != null)
            {
                if (instance.BTRSettings.LocationsWithBTR.Contains(location.Id))
                {
                    Logger.LogInfo("Loading BTR data...");
#if DEBUG
                    Logger.LogWarning("Spawning BTR controller and setting spawn chance to 100%");
                    var settings = Singleton<GlobalConfiguration>.Instance.BTRLocalSettings;
                    var mapSettings = settings.ServerMapBTRSettings.First(x => x.Value.MapID == gameWorld.LocationId);
                    var btrSettings = mapSettings.Value;
                    btrSettings.ChanceSpawn = 100;
                    btrSettings.SpawnPeriod = new(5, 10);
                    btrSettings.MoveSpeed = 32f;
                    btrSettings.PauseDurationRange = new(595, 600);
                    settings.ServerMapBTRSettings[mapSettings.Key] = btrSettings;
#endif
                    gameWorld.BtrController = new BtrController(gameWorld);
                    if (IsServer)
                    {
                        _btrSpawn = GlobalEventsController.Instance.SubscribeOnEvent<BtrSpawnOnThePathEvent>(OnBtrSpawn);
                    }
                }
            }
            else
            {
                Logger.LogError("InitBTRController::GlobalConfiguration was missing when initializing BTR!");
            }
        }
    }

    /// <summary>
    /// Event handler triggered when a BTR spawns on its path, broadcasting the spawn to all connected clients.
    /// </summary>
    /// <param name="spawnEvent">The BTR spawn event data.</param>
    private void OnBtrSpawn(BtrSpawnOnThePathEvent spawnEvent)
    {
        Logger.LogInfo("BTR spawned, notifying clients");
        Singleton<IFikaNetworkManager>.Instance.SendGenericPacket(EGenericSubPacketType.SpawnBTR,
            BtrSpawn.FromValue(spawnEvent.Position, spawnEvent.Rotation, spawnEvent.PlayerProfileId), true);
    }

    /// <summary>
    /// Initializes the transit system.
    /// </summary>
    /// <param name="gameWorld">The game world instance.</param>
    /// <param name="instance">The global configuration containing transit settings.</param>
    /// <param name="profile">The player profile.</param>
    /// <param name="localRaidSettings">The local raid settings.</param>
    /// <param name="location">The current raid location definition.</param>
    public virtual void InitializeTransitSystem(GameWorld gameWorld, GlobalConfiguration instance, Profile profile,
        LocalRaidSettings localRaidSettings, LocationSettings.Location location)
    {
        bool transitActive;
        if (instance == null)
        {
            transitActive = false;
        }
        else
        {
            var transitSettings = instance.transitSettings;
            transitActive = transitSettings != null && transitSettings.active;
        }
        if (transitActive)
        {
            gameWorld.TransitController = IsServer ? new FikaHostTransitController(instance.transitSettings, location.transitParameters,
                profile, localRaidSettings) : new ClientTransitController(instance.transitSettings, location.transitParameters,
                profile, localRaidSettings);

            if (gameWorld.TransitController is FikaHostTransitController fikaHostTransitController)
            {
                fikaHostTransitController.PostConstruct();
            }
        }
        else
        {
            Logger.LogInfo("Transits are disabled");
            EFT.TransitController.DisableTransitPoints();
        }
    }

    /// <summary>
    /// Initializes the Runddans holiday event controller and environment for the current location if active.
    /// </summary>
    /// <param name="instance">The global configuration instance.</param>
    /// <param name="gameWorld">The game world instance.</param>
    /// <param name="location">The current raid location definition.</param>
    public void InitializeRunddans(GlobalConfiguration instance, GameWorld gameWorld, LocationSettings.Location location)
    {
        // TODO: Add christmas event
        bool runddansActive;
        if (instance == null)
        {
            runddansActive = false;
        }
        else
        {
            var runddansSettings = instance.runddansSettings;
            runddansActive = runddansSettings != null && runddansSettings.active;
        }
        if (runddansActive)
        {
            gameWorld.RunddansController = IsServer ? new HostRunddansController(instance.runddansSettings, location)
                : new ClientRunddansController(instance.runddansSettings, location);
            Logger.LogInfo("Created RunddansController");
        }
        else
        {
            RunddansController.ToggleEventEnvironment(false);
        }
    }

    /// <summary>
    /// Asynchronously initializes or synchronizes loot items within the current raid location.
    /// </summary>
    /// <param name="location">The current raid location definition.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public abstract Task InitializeLoot(LocationSettings.Location location);

    /// <summary>
    /// Configures and updates the raid match code.
    /// </summary>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task SetupRaidCode()
    {
        var raidCode = FikaBackendUtils.RaidCode;
        if (!string.IsNullOrEmpty(raidCode))
        {
            var preloaderUiTraverse = Traverse.Create(MonoBehaviourSingleton<PreloaderUI>.Instance);
            // Raid code
            preloaderUiTraverse.Field("string_3").SetValue($"{raidCode}");
            // Update version label
            preloaderUiTraverse.Method("RefreshCornerLabel").GetValue();

            Logger.LogInfo($"MatchingType: {FikaBackendUtils.ClientType}, Raid Code: {raidCode}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Configures global events and exfiltration points.
    /// </summary>
    /// <param name="player">The player to configure events and exfiltrations for.</param>
    public abstract void SetupEventsAndExfils(Player player);

    /// <summary>
    /// Extracts the specified player from the raid via an exfiltration point or transit point.
    /// </summary>
    /// <param name="player">The player to extract.</param>
    /// <param name="exfiltrationPoint">The exfiltration point used for extraction, if applicable.</param>
    /// <param name="transitPoint">The transit point used for transferring between maps, if applicable.</param>
    public abstract void Extract(FikaPlayer player, ExfiltrationPoint exfiltrationPoint, TransitPoint transitPoint = null);

    /// <summary>
    /// Used to make sure no stims or mods reset the DamageCoeff
    /// </summary>
    /// <param name="player">The <see cref="FikaPlayer"/> to run the coroutine on.</param>
    /// <param name="coopGame">The active <see cref="CoopGame"/> instance.</param>
    /// <returns>An enumerator for coroutine progression.</returns>
    protected IEnumerator ExtractRoutine(FikaPlayer player, CoopGame coopGame)
    {
        WaitForEndOfFrame waitForEndOfFrame = new();
        while (coopGame.Status != GameStatus.Stopping)
        {
            if (player != null && player.ActiveHealthController != null)
            {
                if (player.ActiveHealthController.DamageCoeff != 0)
                {
                    player.ActiveHealthController.SetDamageCoeff(0);
                }
            }
            else
            {
                yield break;
            }
            yield return waitForEndOfFrame;
        }
    }

    /// <summary>
    /// Toggles the <see cref="DebugUI"/> menu
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to enable and display the debug UI; otherwise, <see langword="false"/> to hide it.</param>
    public void ToggleDebug(bool enabled)
    {
        if (_debugUi != null)
        {
            _debugUi.gameObject.SetActive(enabled);
        }
    }

    /// <summary>
    /// Destroys and cleans up the instantiated debug UI GameObject.
    /// </summary>
    public void DestroyDebugComponent()
    {
        if (_debugUi != null)
        {
            ToggleDebug(false);
            GameObject.Destroy(_debugUi.gameObject);
            _debugUi = null;
        }
    }

    /// <summary>
    /// Cleans up controller resources, stopping coroutines, clearing thrown grenades, and resetting raid settings.
    /// </summary>
    public virtual void CleanUp()
    {
        ThrownGrenades?.Clear();

        if (_extractRoutine != null)
        {
            _abstractGame.StopCoroutine(_extractRoutine);
        }

        if (!FikaBackendUtils.IsTransit)
        {
            FikaBackendUtils.CustomRaidSettings.Reset();
        }
    }

    /// <summary>
    /// Initializes bot spawning subsystems and begins the pre-raid countdown.
    /// </summary>
    /// <param name="controllerSettings">The bot controller settings.</param>
    /// <param name="gameWorld">The game world instance.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public abstract Task StartBotSystemsAndCountdown(BotControllerSettings controllerSettings, GameWorld gameWorld);

    /// <summary>
    /// Updates client-side raid time and DateTime according to server time values.
    /// </summary>
    /// <param name="gameTime">The current in-raid game time.</param>
    /// <param name="sessionTime">The elapsed or remaining session time.</param>
    /// <param name="gameDateTime">The updated <see cref="GameDateTime"/> instance.</param>
    public void SetClientTime(DateTime gameTime, TimeSpan sessionTime, GameDateTime gameDateTime)
    {
        GameTime = gameTime;
        SessionTime = sessionTime;
        if (_abstractGame is CoopGame coopGame)
        {
            Logger.LogInfo($"Received date from server, was [{coopGame.GameDateTime.Calculate():G}] - new [{gameDateTime.Calculate():G}]");
            coopGame.GameDateTime = gameDateTime;
            coopGame.GameWorld.GameDateTime = gameDateTime;
        }
    }
}