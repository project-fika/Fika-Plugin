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
using static LocationSettingsClass;

namespace Fika.Core.Main.GameMode;

public abstract class BaseGameController
{
    public BaseGameController(IFikaGame game, EUpdateQueue updateQueue, GameWorld gameWorld, ISession session)
    {
        _fikaGame = game;
        _abstractGame = (AbstractGame)game;
        _updateQueue = updateQueue;
        _gameWorld = gameWorld;
        _backendSession = session;
        IsServer = FikaBackendUtils.IsServer;

        Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
    }

    public ManualLogSource Logger { get; set; }

    public AbstractGame GameInstance
    {
        get
        {
            return _abstractGame;
        }
    }

    protected IFikaGame _fikaGame;
    protected AbstractGame _abstractGame;

    public bool IsServer { get; }
    public bool RaidStarted { get; internal set; }
    public FikaTimeManager TimeManager { get; set; }

    // Weather
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
    public bool WeatherReady { get; internal set; }
    public WeatherClass[] WeatherClasses { get; set; }
    public SeasonsSettingsClass SeasonsSettings { get; set; }
    public FikaExfilManager ExfilManager { get; set; }

    // Raid data
    public List<ThrowWeapItemClass> ThrownGrenades { get; set; }
    public RaidSettings RaidSettings { get; set; }
    public GClass1404 LootItems { get; set; } = [];
    public LocationSettingsClass.Location Location { get; set; }
    public Dictionary<string, Player> Bots = [];
    public CoopHandler CoopHandler
    {
        get
        {
            return _coopHandler;
        }
    }
    public DateTime? GameTime { get; set; }
    public TimeSpan? SessionTime { get; set; }
    public List<string> LocalTriggerZones
    {
        get
        {
            return _localTriggerZones;
        }
    }
    protected List<string> _localTriggerZones = [];

    // Spawns
    public Vector3 ClientSpawnPosition { get; set; }
    public Quaternion ClientSpawnRotation { get; set; }
    public ISpawnSystem SpawnSystem { get; set; }
    public string InfiltrationPoint { get; set; }
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

    protected SpawnPointManagerClass _spawnPoints;
    protected ISpawnPoint _spawnPoint;
    protected Action _btrSpawn;
    protected CoopHandler _coopHandler;
    protected FikaPlayer _localPlayer;
    protected EUpdateQueue _updateQueue;
    protected GameWorld _gameWorld;
    protected ISession _backendSession;
    protected Coroutine _extractRoutine;

    public void SetLocalPlayer(FikaPlayer player)
    {
        _localPlayer = player;
        _coopHandler.MyPlayer = player;
    }

    /// <summary>
    /// <see cref="Task"/> used to wait for host to start the raid
    /// </summary>
    /// <returns></returns>
    public virtual Task WaitForHostToStart()
    {
        Logger.LogInfo("Starting task to wait for host to start the raid.");
        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_HOST_START_RAID.Localized());

        return Task.CompletedTask;
    }

    /// <summary>
    /// This creates a "custom" Back button so that we can back out if we get stuck
    /// </summary>
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

    public abstract IEnumerator WaitForHostInit(int timeBeforeDeployLocal);

    public Vector3 GetSpawnPosition()
    {
        return IsServer ? _spawnPoint.Position : ClientSpawnPosition;
    }

    public Quaternion GetSpawnRotation()
    {
        return IsServer ? _spawnPoint.Rotation : ClientSpawnRotation;
    }

    public virtual void CreateDebugComponent()
    {
        var asset = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.DebugUI);
        var debugObject = GameObject.Instantiate(asset);
        _debugUi = debugObject.GetComponent<DebugUI>();
        debugObject.SetActive(false);
    }

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
    public abstract Task WaitForOtherPlayersToLoad();

    /// <summary>
    /// Runs a few last changes to the raid setup
    /// </summary>
    public virtual IEnumerator FinishRaidSetup()
    {
        LoadingScreenUI.Instance.UpdateAndBroadcast(90f);

        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_FINISHING_RAID_INIT.Localized());

        WaitForEndOfFrame endOfFrame = new();
        var musicTask = Singleton<GUISounds>.Instance.method_10(false, CancellationToken.None);
        while (!musicTask.IsCompleted)
        {
            yield return endOfFrame;
        }

        GClass2313.ResetAudioBuffer();

        _gameWorld.TriggersModule = _abstractGame.gameObject.AddComponent<LocalFikaTriggersModule>();
        _gameWorld.FillLampControllers();
        if (Location.Id == "laboratory")
        {
            Season = ESeason.Summer;
        }
        WeatherReady = true;
        FikaBackendUtils.CustomRaidSettings.UseCustomWeather = false;

        Class444 seasonController = new();
        _gameWorld.GInterface29_0 = seasonController;

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

    public abstract Task GenerateWeathers();

    public virtual IEnumerator CountdownScreen(Profile profile, string profileId)
    {
        FikaBackendUtils.GroupPlayers.Clear();

        var timeBeforeDeployLocal = FikaBackendUtils.IsReconnect ? 3 : Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal;
#if DEBUG
        timeBeforeDeployLocal = 3;
#endif
        yield return WaitForHostInit(timeBeforeDeployLocal);

        NetManagerUtils.DisableLoadingScreenUI();

        var dateTime = EFTDateTimeClass.Now.AddSeconds(timeBeforeDeployLocal);
        new MatchmakerFinalCountdown.FinalCountdownScreenClass(profile, dateTime).ShowScreen(EScreenState.Root);
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

    private void SyncTransitControllers(string profileId)
    {
        var transitController = Singleton<GameWorld>.Instance.TransitController;
        if (transitController == null)
        {
            if (FikaPlugin.Instance.EnableTransits)
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

    public abstract Task ReceiveSpawnPoint(Profile profile);

    public abstract void CreateSpawnSystem(Profile profile);

    public void InitShellingController(BackendConfigSettingsClass instance, GameWorld gameWorld, LocationSettingsClass.Location location)
    {
        if (instance != null && instance.ArtilleryShelling != null && instance.ArtilleryShelling.ArtilleryMapsConfigs?.Keys.Contains(location.Id) == true)
        {
            if (IsServer)
            {
                gameWorld.ServerShellingController = new ServerShellingControllerClass();
            }
            gameWorld.ClientShellingController = new ClientShellingControllerClass(IsServer);
        }
    }

    public void InitHalloweenEvent(BackendConfigSettingsClass instance, GameWorld gameWorld, LocationSettingsClass.Location location)
    {
        if (instance != null && instance.EventSettings.EventActive && !instance.EventSettings.LocationsToIgnore.Contains(location.Id))
        {
#if DEBUG
            Logger.LogWarning("Spawning halloween prefabs");
#endif
            gameWorld.HalloweenEventController = new HalloweenEventControllerClass();
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

    public void InitBTRController(BackendConfigSettingsClass instance, GameWorld gameWorld, LocationSettingsClass.Location location)
    {
        if (FikaPlugin.Instance.UseBTR)
        {
            if (instance != null)
            {
                if (instance.BTRSettings.LocationsWithBTR.Contains(location.Id))
                {
                    Logger.LogInfo("Loading BTR data...");
#if DEBUG
                    Logger.LogWarning("Spawning BTR controller and setting spawn chance to 100%");
                    var settings = Singleton<BackendConfigSettingsClass>.Instance.BTRLocalSettings;
                    var mapSettings = settings.ServerMapBTRSettings.First(x => x.Value.MapID == gameWorld.LocationId);
                    var btrSettings = mapSettings.Value;
                    btrSettings.ChanceSpawn = 100;
                    btrSettings.SpawnPeriod = new(5, 10);
                    btrSettings.MoveSpeed = 32f;
                    btrSettings.PauseDurationRange = new(595, 600);
                    settings.ServerMapBTRSettings[mapSettings.Key] = btrSettings;
#endif
                    gameWorld.BtrController = new BTRControllerClass(gameWorld);
                    if (IsServer)
                    {
                        _btrSpawn = GlobalEventHandlerClass.Instance.SubscribeOnEvent<BtrSpawnOnThePathEvent>(OnBtrSpawn);
                    }
                }
            }
            else
            {
                Logger.LogError("InitBTRController::BackendConfigSettingsClass was missing when initializing BTR!");
            }
        }
    }

    private void OnBtrSpawn(BtrSpawnOnThePathEvent spawnEvent)
    {
        Logger.LogInfo("BTR spawned, notifying clients");
        Singleton<IFikaNetworkManager>.Instance.SendGenericPacket(EGenericSubPacketType.SpawnBTR,
            BtrSpawn.FromValue(spawnEvent.Position, spawnEvent.Rotation, spawnEvent.PlayerProfileId), true);
    }

    /// <summary>
    /// Initializes the transit system TODO: Add headless variant
    /// </summary>
    /// <param name="gameWorld"></param>
    /// <param name="instance"></param>
    /// <param name="profile"></param>
    /// <param name="localRaidSettings"></param>
    /// <param name="location"></param>
    public virtual void InitializeTransitSystem(GameWorld gameWorld, BackendConfigSettingsClass instance, Profile profile,
        LocalRaidSettings localRaidSettings, LocationSettingsClass.Location location)
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
            TransitControllerAbstractClass.DisableTransitPoints();
        }
    }

    public void InitializeRunddans(BackendConfigSettingsClass instance, GameWorld gameWorld, LocationSettingsClass.Location location)
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
            RunddansControllerAbstractClass.ToggleEventEnvironment(false);
        }
    }

    public abstract Task InitializeLoot(LocationSettingsClass.Location location);

    public Task SetupRaidCode()
    {
        var raidCode = FikaBackendUtils.RaidCode;
        if (!string.IsNullOrEmpty(raidCode))
        {
            var preloaderUiTraverse = Traverse.Create(MonoBehaviourSingleton<PreloaderUI>.Instance);
            // Raid code
            preloaderUiTraverse.Field("string_3").SetValue($"{raidCode}");
            // Update version label
            preloaderUiTraverse.Method("method_6").GetValue();

            Logger.LogInfo($"MatchingType: {FikaBackendUtils.ClientType}, Raid Code: {raidCode}");
        }

        return Task.CompletedTask;
    }

    public abstract void SetupEventsAndExfils(Player player);

    public abstract void Extract(FikaPlayer player, ExfiltrationPoint exfiltrationPoint, TransitPoint transitPoint = null);

    /// <summary>
    /// Used to make sure no stims or mods reset the DamageCoeff
    /// </summary>
    /// <param name="player">The <see cref="FikaPlayer"/> to run the coroutine on</param>
    /// <returns></returns>
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
    /// <param name="enabled"></param>
    public void ToggleDebug(bool enabled)
    {
        if (_debugUi != null)
        {
            _debugUi.gameObject.SetActive(enabled);
        }
    }

    public void DestroyDebugComponent()
    {
        if (_debugUi != null)
        {
            ToggleDebug(false);
            GameObject.Destroy(_debugUi.gameObject);
            _debugUi = null;
        }
    }

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

    public abstract Task StartBotSystemsAndCountdown(BotControllerSettings controllerSettings, GameWorld gameWorld);

    public void SetClientTime(DateTime gameTime, TimeSpan sessionTime, GameDateTime gameDateTime)
    {
        GameTime = gameTime;
        SessionTime = sessionTime;
        if (_abstractGame is CoopGame coopGame)
        {
            Logger.LogInfo($"Received date from server, was [{coopGame.GameDateTime.Calculate():G}] - new [{gameDateTime.Calculate():G}]");
            coopGame.GameDateTime = gameDateTime;
            coopGame.GameWorld_0.GameDateTime = gameDateTime;
        }
    }
}