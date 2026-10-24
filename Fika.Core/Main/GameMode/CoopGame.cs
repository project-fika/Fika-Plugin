using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audio.RadioSystem;
using BepInEx.Logging;
using Comfort.Common;
using CommonAssets.Scripts.Game;
using Dissonance.Integrations.MirrorIgnorance;
using Dissonance.Networking.Client;
using Diz.Jobs;
using EFT;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using EFT.Airdrop;
using EFT.AssetsManager;
using EFT.Bots;
using EFT.CameraControl;
using EFT.Communications;
using EFT.EnvironmentEffect;
using EFT.Game.Spawning;
using EFT.HealthSystem;
using EFT.InputSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Insurance;
using EFT.UI.Screens;
using EFT.Utilities;
using EFT.Weather;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.Components;
using Fika.Core.Main.Patches.BTR;
using Fika.Core.Main.Players;
using Fika.Core.Main.Patches.LocalGame;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.World;
using Fika.Core.UI.Models;
using HarmonyLib;
using JsonType;
using EFT.UI.Matchmaker;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.GameMode;

/// <summary>
/// Coop game used in Fika
/// </summary>
public sealed class CoopGame : BaseLocalGame<EftGamePlayerOwner>, IFikaGame, IClientHearingTable
{
    public CoopGame(IntPtr pointer) : base(pointer)
    {
    }

    /// <summary>
    /// Invoked when the main player extracts
    /// </summary>
    /// <remarks>Does not run on headless clients as there is no player</remarks>
    public static Action<FikaPlayer> MainPlayerExtracted { get; set; }

    public BaseGameController GameController { get; set; }
    public ExitStatus ExitStatus { get; set; } = ExitStatus.Survived;
    public string ExitLocation { get; set; }
    public List<int> ExtractedPlayers { get; } = [];

    public ESeason Season
    {
        get
        {
            return GameController.Season;
        }
        set
        {
            GameController.Season = value;
        }
    }

    public SeasonsSettings SeasonsSettings
    {
        get
        {
            return GameController.SeasonsSettings;
        }

        set
        {
            GameController.SeasonsSettings = value;
        }
    }

    private static ManualLogSource _logger;

    private Func<LocalPlayer, EftGamePlayerOwner> _func_1;
    private FikaPlayer _localFikaPlayer;
    private bool _hasSaved;
    private float _voipDistance;

    /// <summary>
    /// Creates a <see cref="CoopGame"/>
    /// </summary>
    internal static CoopGame Create(IInputTree inputTree, Profile profile, GameWorld gameWorld, GameDateTime backendDateTime,
        InsuranceCompany insurance, GameUI gameUI, LocationSettings.Location location,
        TimeAndWeatherSettings timeAndWeather, WavesSettings wavesSettings, EDateTime dateTime,
        Callback<ExitStatus, Il2CppSystem.TimeSpan, ClientMetrics> callback, float fixedDeltaTime, EUpdateQueue updateQueue,
        IEftSession backEndSession, TimeSpan sessionTime, ClientMetricsEvents metricsEvents,
        ClientMetricsCollector metricsCollector, LocalRaidSettings localRaidSettings, RaidSettings raidSettings)
    {
        _logger = Logger.CreateLogSource("CoopGame");

        FikaGlobals.NetworkManager.RaidSide = localRaidSettings.playerSide;

        var gameTime = backendDateTime;
        if (timeAndWeather.HourOfDay != -1)
        {
            _logger.LogInfo($"Using custom time, hour of day: {timeAndWeather.HourOfDay}");
            var currentTime = backendDateTime.StatedGameDateTime;
            DateTime newTime = new(currentTime.Year, currentTime.Month, currentTime.Day, timeAndWeather.HourOfDay,
                currentTime.Minute, currentTime.Second, currentTime.Millisecond);
            gameTime = new(backendDateTime.StatedRealDateTime, (newTime).ToIl2Cpp(), backendDateTime.TimeFactor);
            gameTime.Reset((newTime).ToIl2Cpp());
            dateTime = EDateTime.CURR;
        }

        var coopGame = Create<CoopGame>(inputTree, profile, gameWorld, gameTime, insurance, gameUI,
            location, timeAndWeather, wavesSettings, dateTime, callback, fixedDeltaTime, updateQueue, backEndSession,
            new Il2CppSystem.Nullable<Il2CppSystem.TimeSpan>(sessionTime.ToIl2Cpp()), metricsEvents, metricsCollector, localRaidSettings);
        coopGame.GameController = FikaBackendUtils.IsServer ? new HostGameController(coopGame, updateQueue, gameWorld, backEndSession, location, wavesSettings, coopGame.GameDateTime)
            : new ClientGameController(coopGame, updateQueue, gameWorld, backEndSession);
        coopGame.GameController.Location = location;

        float hearingDistance = FikaGlobals.VOIPHandler.PushToTalkSettings.HearingDistance;
        coopGame._voipDistance = (hearingDistance * hearingDistance) + 9;

        ClientHearingTable.Instance = coopGame;

        if (coopGame.GameController.IsServer)
        {
            gameWorld.World.RegisterNetworkInteractionObjects();
        }

        if (timeAndWeather.TimeFlowType != ETimeFlowType.x1)
        {
            var newFlow = timeAndWeather.TimeFlowType.ToTimeFlow();
            coopGame.GameWorld.GameDateTime.TimeFactor = newFlow;
            _logger.LogInfo($"Using custom time flow: {newFlow}");
        }

        if (FikaBackendUtils.CustomRaidSettings.UseCustomWeather && coopGame.GameController.IsServer)
        {
            _logger.LogInfo("Custom weather enabled, initializing curves");
            (coopGame.GameController as HostGameController).SetupCustomWeather(timeAndWeather);
        }

        SetupGamePlayerOwnerHandler setupGamePlayerOwnerHandler = new(inputTree, insurance, backEndSession, gameUI, coopGame, location);
        coopGame._func_1 = setupGamePlayerOwnerHandler.HandleSetup;

        FikaGlobals.FikaGame = coopGame;
        FikaEventDispatcher.DispatchEvent(new FikaGameCreatedEvent(coopGame));

        var endByExitTrigger = coopGame.GetComponent<EndByExitTrigerScenario>();
        var endByTimerScenario = coopGame.GetComponent<EndByTimerScenario>();

        if (endByExitTrigger != null)
        {
            Destroy(endByExitTrigger);
        }
        if (endByTimerScenario != null)
        {
            Destroy(endByTimerScenario);
        }

        coopGame.GameController.TimeManager = FikaTimeManager.Create(coopGame);
        coopGame.GameController.RaidSettings = raidSettings;
        coopGame.GameController.ThrownGrenades = [];

        return coopGame;
    }

    /// <summary>
    /// Used to create a <see cref="EftGamePlayerOwner"/>
    /// </summary>
    /// <param name="inputTree"></param>
    /// <param name="insurance"></param>
    /// <param name="backEndSession"></param>
    /// <param name="gameUI"></param>
    /// <param name="game"></param>
    /// <param name="location"></param>
    private class SetupGamePlayerOwnerHandler(IInputTree inputTree, InsuranceCompany insurance, IEftSession backEndSession, GameUI gameUI, CoopGame game, LocationSettings.Location location)
    {
        private readonly IInputTree _inputTree = inputTree;
        private readonly InsuranceCompany _insurance = insurance;
        private readonly IEftSession _backEndSession = backEndSession;
        private readonly GameUI _gameUI = gameUI;
        private readonly CoopGame _game = game;
        private readonly LocationSettings.Location _location = location;

        public EftGamePlayerOwner HandleSetup(LocalPlayer player)
        {
            _game.LocalPlayer = player;
            var gamePlayerOwner = EftGamePlayerOwner.Create(player, _inputTree, _insurance, _backEndSession,
                _gameUI, _game.GameDateTime, _location);
            gamePlayerOwner.OnLeave += new Action(_game.LocalLeaveGame);
            return gamePlayerOwner;
        }
    }

    internal ScreenUpdater ScreenUpdater { get; set; }

    public override void SetMatchmakerStatus(EMatchingStage stage, Il2CppSystem.Nullable<float> progress)
    {
        if (ScreenUpdater != null)
        {
            ScreenUpdater.UpdateStatus(stage, progress);
            return;
        }
        base.SetMatchmakerStatus(stage, progress);
    }

    public void SetMatchmakerStatus(string status, float? progress = null)
    {
        Main.Patches.LocalGame.ScreenUpdater.SetStatusText(status);
        SetMatchmakerStatus(EMatchingStage.None,
            progress.HasValue ? new Il2CppSystem.Nullable<float>(progress.Value) : new Il2CppSystem.Nullable<float>());
    }

    /// <summary>
    /// The countdown deploy screen
    /// </summary>
    public override Il2CppSystem.Collections.IEnumerator InitSessionUI()
    {
        return InitSessionUIRoutine().ToIl2Cpp();
    }

    private IEnumerator InitSessionUIRoutine()
    {
        yield return GameController.CountdownScreen(Profile, ProfileId);
    }

    public override Il2CppSystem.Threading.Tasks.Task<LocalPlayer> PlayerFactory(GameWorld gameWorld, int playerId, Vector3 position, Quaternion rotation, string layerName, string prefix, EPointOfView pointOfView, Profile profile, bool aiControl, EUpdateQueue updateQueue, Player.EUpdateMode armsUpdateMode, Player.EUpdateMode bodyUpdateMode, CharacterControllerSpawner.Mode characterControllerMode, Il2CppSystem.Func<float> getSensitivity, Il2CppSystem.Func<float> getAimingSensitivity, IStatisticsManager statisticsManager, IEftSession session, ELocalMode localMode)
    {
        return PlayerFactoryAsync(gameWorld, playerId, position, rotation, layerName, prefix, pointOfView, profile, aiControl, updateQueue, armsUpdateMode, bodyUpdateMode, characterControllerMode, getSensitivity, getAimingSensitivity, statisticsManager, session, localMode).ToIl2Cpp();
    }

    private async Task<LocalPlayer> PlayerFactoryAsync(GameWorld gameWorld, int playerId, Vector3 position, Quaternion rotation, string layerName, string prefix, EPointOfView pointOfView, Profile profile, bool aiControl, EUpdateQueue updateQueue, Player.EUpdateMode armsUpdateMode, Player.EUpdateMode bodyUpdateMode, CharacterControllerSpawner.Mode characterControllerMode, Il2CppSystem.Func<float> getSensitivity, Il2CppSystem.Func<float> getAimingSensitivity, IStatisticsManager statisticsManager, IEftSession session, ELocalMode localMode)
    {
        if (!TransitController.IsTransit(profile.Id, out int _) && !FikaBackendUtils.IsReconnect)
        {
            profile.SetSpawnedInSession(false);
        }

        var fikaPlayer = await FikaPlayer.Create(gameWorld, playerId, position, rotation, "Player", "Main_", EPointOfView.FirstPerson,
            profile, false, UpdateQueue, armsUpdateMode, Player.EUpdateMode.Auto,
            AppEnvironment.Config.CharacterController.ClientPlayerMode, getSensitivity, getAimingSensitivity,
            statisticsManager, new ClientViewFilter(), session, playerId, FikaGlobals.NetworkManager.StrictInventorySync);

        fikaPlayer.Location = Location.Id;
        var coopHandler = GameController.CoopHandler;

        if (coopHandler == null)
        {
            _logger.LogError("vmethod_3: CoopHandler was null!");
            throw new InvalidOperationException("CoopHandler was missing during CoopGame init");
        }

        if (GameController.RaidSettings.MetabolismDisabled)
        {
            fikaPlayer.HealthController.DisableMetabolism();
            FikaGlobals.DisplayMessage(LocaleUtils.METABOLISM_DISABLED.Localized(), iconType: EFT.Communications.ENotificationIconType.Alert);
        }

        coopHandler.Players.Add(fikaPlayer.NetId, fikaPlayer);
        coopHandler.HumanPlayers.Add(fikaPlayer);
        fikaPlayer.SetupMainPlayer();

        PlayerSpawnRequest body = new(fikaPlayer.ProfileId, FikaBackendUtils.GroupId);
        await FikaRequestHandler.UpdatePlayerSpawn(body);

        fikaPlayer.SpawnPoint = GameController.SpawnPoint;

        await NetManagerUtils.SetupGameVariables(fikaPlayer);

        if (!GameController.IsServer && !FikaBackendUtils.IsReconnect)
        {
            var playerInfoPacket = new PlayerInfoPacket()
            {
                Profile = fikaPlayer.Profile,
                ControllerId = fikaPlayer.InventoryController.CurrentId,
                FirstOperationId = fikaPlayer.InventoryController.NextOperationId
            };

            if (fikaPlayer.ActiveHealthController != null)
            {
                playerInfoPacket.HealthByteArray = fikaPlayer.ActiveHealthController.SerializeState();
            }

            if (fikaPlayer.HandsController != null)
            {
                playerInfoPacket.ControllerType = HandsControllerTypeConvert.FromController(fikaPlayer.HandsController);
                playerInfoPacket.ItemId = fikaPlayer.HandsController.Item.Id;
                playerInfoPacket.IsStationary = fikaPlayer.MovementContext.IsStationaryWeaponInHands;
            }

            var packet = new SendCharacterPacket(playerInfoPacket, fikaPlayer.HealthController.IsAlive, false, fikaPlayer.Transform.position, fikaPlayer.NetId);
            var client = Singleton<FikaClient>.Instance;

            client.SendGenericPacket(in packet, DeliveryMethod.ReliableOrdered, true);
        }

        _logger.LogInfo("Adding debug component...");
        GameController.CreateDebugComponent();

        if (FikaBackendUtils.IsReconnect && !FikaBackendUtils.ReconnectPosition.Equals(Vector3.zero))
        {
            fikaPlayer.Teleport(FikaBackendUtils.ReconnectPosition);
            fikaPlayer.MovementContext.Rotation = FikaBackendUtils.ReconnectRotation;
            fikaPlayer.MovementContext.CachedRotation = FikaBackendUtils.ReconnectRotation;
        }

        return fikaPlayer;
    }

    private Action StopFromCancel(Player myPlayer)
    {
        StopFromCancel(myPlayer.ProfileId, ExitStatus.Runner);
        PlayerLeftRequest playerLeftRequest = new(FikaBackendUtils.Profile.ProfileId);
        FikaRequestHandler.RaidLeave(playerLeftRequest);
        return null;
    }

    /// <summary>
    /// Initializes the local player, replaces <see cref="BaseLocalGame{TPlayerOwner}.method_4(BotControllerSettings, string, InventoryController)"/>
    /// </summary>
    public async Task InitPlayer(BotControllerSettings botsSettings)
    {
        Status = GameStatus.Running;
        UnityEngine.Random.InitState((int)DateTimeExtensions.Now.Ticks);

        if (!GameController.IsServer)
        {
            await (GameController as ClientGameController).WaitForHostToLoad();
        }

        _logger.LogInfo("Creating CoopHandler");
        await GameController.SetupCoopHandler(this);

        var gameWorld = Singleton<GameWorld>.Instance;
        gameWorld.LocationId = Location.Id;

        _logger.LogInfo($"Initializing Exfils: Id {Location.Id}, Exits: {Location.exits?.Length ?? 0}, SecretExits: {Location.SecretExits?.Length ?? 0}");

        ExfiltrationController.Instance.InitAllExfiltrationPoints(Location._Id, Location.exits, Location.SecretExits,
            !GameController.IsServer, Location.DisabledScavExits);

        _logger.LogInfo($"Location: {Location.Name}");
        var instance = Singleton<GlobalConfiguration>.Instance;

        GameController.InitWorldControllers(gameWorld);
        GameController.InitShellingController(instance, gameWorld, Location);
        GameController.InitHalloweenEvent(instance, gameWorld, Location);
        GameController.InitBTRController(instance, gameWorld, Location);

        if (FikaPlugin.Instance.Settings.EnableTransits)
        {
            GameController.InitializeTransitSystem(gameWorld, instance, Profile, _raidSettings, Location);
        }

        GameController.InitializeRunddans(instance, gameWorld, Location);
        GameController.InitializePasscodes(gameWorld, Location);

        if (GameController.IsServer)
        {
            Singleton<FikaServer>.Instance.RaidInitialized = true;
        }

        gameWorld.ClientBroadcastSyncController = new ClientBroadcastSyncController();

        var config = AppEnvironment.Config;
        if (config.FixedFrameRate > 0f)
        {
            FixedDeltaTime = 1f / config.FixedFrameRate;
        }

        if (FikaBackendUtils.IsReconnect)
        {
            await GetReconnectProfile(ProfileId);
        }

        try
        {
            var player = await CreateLocalPlayer()
                ?? throw new NullReferenceException("InitPlayer: Player was null!");
            _players.Add(player.ProfileId, player);
            _playerOwner = _func_1(player);
            PlayerCameraController.Create(_playerOwner.Player);
            CameraManager.Instance.SetOcclusionCullingEnabled(Location.OcculsionCullingEnabled);
            CameraManager.Instance.IsActive = false;

            FikaGlobals.NetworkManager.CreateFikaChat();
            if (player is FikaPlayer fikaPlayer)
            {
                QuestTemplateSync.SendLocal(fikaPlayer);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"InitPlayer: {ex.Message}");
            throw;
        }

        await GameController.WaitForHostToStart();

        var location = _raidSettings.selectedLocation;
        await GameController.InitializeLoot(location);
        await SpawnLoot(location);

        GameController.CoopHandler.ShouldSync = true;

        if (FikaBackendUtils.IsReconnect)
        {
            await Reconnect();
            foreach (var item in _playerOwner.Player.ActiveHealthController.BodyState)
            {
                if (item.Value.Health.AtMinimum)
                {
                    item.Value.IsDestroyed = true;
                }
            }
        }

        await LocalBotsControllerInitialization(botsSettings, null).AsManaged();

        if (GameController.IsServer)
        {
            Singleton<IBotGame>.Instance.BotsController.CoversData.Patrols.RestoreLoot(Location.Loot, LocationScene.GetAllObjects<LootableContainer>(false));
            ServerAirdropManager airdropEventClass = new()
            {
                AirdropParameters = Location.airdropParameters
            };
            airdropEventClass.Init(true);
            (Singleton<GameWorld>.Instance as ClientGameWorld).ClientSynchronizableObjectLogicProcessor.ServerAirdropManager = airdropEventClass;
            GameWorld.SynchronizableObjectLogicProcessor.AirdropDataSender = Singleton<FikaServer>.Instance;
        }
        if (Location.FixedWeatherLocationConfig != null)
        {
            SetFixedWeather(Location.FixedWeatherLocationConfig);
        }

        await PrepareSession();
        FikaEventDispatcher.DispatchEvent(new GameWorldStartedEvent(GameWorld));
    }

    private async Task GetReconnectProfile(string profileId)
    {
        Profile = null;

        ReconnectPacket reconnectPacket = new()
        {
            IsRequest = true,
            InitialRequest = true,
            ProfileId = profileId
        };
        var client = Singleton<FikaClient>.Instance;
        client.SendData(ref reconnectPacket, DeliveryMethod.ReliableOrdered);

        do
        {
            await Task.Delay(250);
        } while (Profile == null);

        await Singleton<ObjectsFactory>.Instance.LoadBundlesAndCreatePools(ObjectsFactory.PoolsCategory.Raid, ObjectsFactory.AssemblyType.Local,
            Profile.GetAllPrefabPaths(true).ToIl2CppList(), JobYieldPriority.General, null, new Il2CppSystem.Threading.CancellationToken());
    }

    private async Task Reconnect()
    {
        SetMatchmakerStatus(LocaleUtils.UI_RECONNECTING.Localized());

        ReconnectPacket reconnectPacket = new()
        {
            IsRequest = true,
            ProfileId = ProfileId
        };
        var client = Singleton<FikaClient>.Instance;
        client.SendData(ref reconnectPacket, DeliveryMethod.ReliableOrdered);

        do
        {
            await Task.Delay(1000);
        } while (!client.ReconnectDone);

        var packet = new ClearSnapshotterPacket
        {
            NetId = client.NetId
        };
        client.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
    }

    private void ConsumeLocationAccessRequirement()
    {
        if (_raidSettings == null || _raidSettings.mode != ELocalMode.PVE_OFFLINE || Location.access == null)
        {
            return;
        }

        var transition = _raidSettings.transition;
        var transitEntry = transition == null || transition.transitionCount <= 0
            ? transition != null && transition.transitionType.HasFlagNoBox(ELocationTransition.Common)
            : true;
        var accessKeyCount = transitEntry ? Location.access.transit_PVE : Location.access.menu_PVE;
        if (accessKeyCount == null || accessKeyCount.Count == 0)
        {
            return;
        }

        var items = Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment);
        if (!LocationAccessRequirementsUtility.TryConsumeAccessRequirement(items, accessKeyCount, null))
        {
            foreach (var entry in accessKeyCount)
            {
                LocationAccessRequirementsUtility.RemoveAllNonKeyItemsByTemplate(items, entry.Key);
            }
        }
    }

    /// <summary>
    /// Creates the local player
    /// </summary>
    /// <returns>A <see cref="Player"/></returns>
    private async Task<LocalPlayer> CreateLocalPlayer()
    {
        Status = GameStatus.Running;

        var num = FikaGlobals.NetworkManager.NetId;

        var eupdateMode = Player.EUpdateMode.Auto;
        if (AppEnvironment.Config.UseHandsFastAnimator)
        {
            eupdateMode = Player.EUpdateMode.Manual;
        }

        if (GameController.IsServer)
        {
            GameController.CreateSpawnSystem(Profile);
        }
        else
        {
            await GameController.ReceiveSpawnPoint(Profile);
            if (string.IsNullOrEmpty(GameController.InfiltrationPoint))
            {
                _logger.LogError("InfiltrationPoint was null after retrieving it from the server!");
                GameController.CreateSpawnSystem(Profile);
            }

            var clientGameController = (ClientGameController)GameController;
            await clientGameController.InitInteractables();
            await clientGameController.InitExfils();
        }

        GameController.ExfilManager = gameObject.AddComponent<FikaExfilManager>();

        ConsumeLocationAccessRequirement();

        if (FikaGlobals.NetworkManager.AllowVOIP)
        {
            _logger.LogInfo("VOIP enabled, initializing...");
            try
            {
                await FikaGlobals.NetworkManager.InitializeVOIP();
            }
            catch (Exception ex)
            {
                _logger.LogError($"There was an error initializing the VOIP module: {ex.Message}");
            }
        }

        IStatisticsManager statisticsManager = new ClientStatisticsManager();

        var spawnPos = GameController.GetSpawnPosition();
        var spawnRot = GameController.GetSpawnRotation();

        LocalPlayer myPlayer;
        try
        {
            if (Profile.Side != EPlayerSide.Savage)
            {
                GenerateNewDogTagId();
            }
            else if (FikaBackendUtils.IsHeadless)
            {
                Profile.Info.Nickname = Profile.Info.Nickname.Insert(0, "headless_");
            }

            if (!FikaBackendUtils.IsReconnect)
            {
                FikaFinalMissionDirector.StripProfile(Profile, num);
            }

            myPlayer = await PlayerFactoryAsync(GameWorld, num, spawnPos, spawnRot, "Player", "", EPointOfView.FirstPerson,
                    Profile, false, UpdateQueue, eupdateMode, Player.EUpdateMode.Auto,
                    AppEnvironment.Config.CharacterController.ClientPlayerMode,
                    new System.Func<float>(FikaGlobals.GetLocalPlayerSensitivity), new System.Func<float>(FikaGlobals.GetLocalPlayerAimingSensitivity), statisticsManager,
                    _backEnd, (_raidSettings?.mode) ?? ELocalMode.TRAINING);
        }
        catch (Exception ex)
        {
            _logger.LogError($"CreateLocalPlayer: {ex.Message}");
            throw;
        }

        myPlayer.OnEpInteraction += OnEpInteraction;

        _localFikaPlayer = myPlayer as FikaPlayer;
        GameController.SetLocalPlayer(_localFikaPlayer);

        _logger.LogInfo("Local player created");
        return myPlayer;
    }

    /// <summary>
    /// Temporary workaround since SPT does not generate a unique ID >3.11 for the dogtag
    /// </summary>
    private void GenerateNewDogTagId()
    {
        var dogTag = Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem;
        if (dogTag != null)
        {
            dogTag._Id_k__BackingField = MongoID.Generate(true);
#if DEBUG
            _logger.LogWarning("Generated new ID for DogTag");
#endif
        }
        else
        {
            _logger.LogError("Could not find DogTag when generating new ID!");
        }
    }

    /// <summary>
    /// Sets the status of the game on the backend
    /// </summary>
    /// <param name="myPlayer"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    private async Task SetStatus(LocalPlayer myPlayer, LobbyEntry.ELobbyStatus status)
    {
        SetStatusModel statusBody = new(myPlayer.ProfileId, status);
        await FikaRequestHandler.UpdateSetStatus(statusBody);
        _logger.LogInfo("Setting game status to: " + status.ToString());
    }

    /// <summary>
    /// Bot System Starter -> Countdown
    /// </summary>
    /// <param name="controllerSettings"></param>
    /// <param name="spawnSystem"></param>
    /// <returns></returns>
    public override Il2CppSystem.Threading.Tasks.Task LocalBotsControllerInitialization(BotControllerSettings controllerSettings, ISpawnSystem spawnSystem)
    {
        return LocalBotsControllerInitializationAsync(controllerSettings, spawnSystem).ToIl2Cpp();
    }

    private async Task LocalBotsControllerInitializationAsync(BotControllerSettings controllerSettings, ISpawnSystem spawnSystem)
    {
        await GameController.StartBotSystemsAndCountdown(controllerSettings, GameWorld);
    }

    public override Il2CppSystem.Collections.IEnumerator SessionRun(Il2CppSystem.Action runCallback)
    {
        return SessionRunRoutine(runCallback).ToIl2Cpp();
    }

    private IEnumerator SessionRunRoutine(Il2CppSystem.Action runCallback)
    {
        yield return GameController.FinishRaidSetup();
        yield return base.SessionRun(runCallback);
    }

    public override void Spawn()
    {
        if (LocalPlayer.ActiveHealthController is ClientHealthController coopClientHealthController)
        {
            coopClientHealthController.Start();
        }
        _playerOwner.Player.HealthController.DiedEvent += HealthController_DiedEvent;
        _playerOwner.Run();
#if DEBUG
        FikaGlobals.LogWarning("Forcing god mode on DEBUG build, use 'god f' console command to disable");
        _playerOwner.Player.ActiveHealthController.SetDamageCoeff(0f);
#endif
    }

    /// <summary>
    /// Sets up <see cref="OfflineHealthController"/> events and all <see cref="ExfiltrationPoint"/>s
    /// </summary>
    public override void SessionRun()
    {
        GameController.SetupEventsAndExfils(_playerOwner.Player);
        SessionStartTime = DateTimeExtensions.Now;
    }

    public override void FixedUpdate()
    {
        // Do nothing
    }

    /// <summary>
    /// Updates a <see cref="ExfiltrationPoint"/> from the server
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enable"></param>
    public void UpdateExfilPointFromServer(ExfiltrationPoint point, bool enable)
    {
        if (GameController.ExfilManager != null)
        {
            GameController.ExfilManager.UpdateExfilPointFromServer(point, enable);
            return;
        }

        _logger.LogError("CoopGame::UpdateExfilPointFromServer: ExfilManager was null!");
    }

    public override void Dispose()
    {
        ClientHearingTable.Instance = null;
        foreach (var player in _players.Values)
        {
            try
            {
                if (player != null)
                {
                    player.Dispose();
                    AssetPoolObject.ReturnToPool(player.gameObject, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }
        _players.Clear();
        base.Dispose();
    }

    /// <summary>
    /// When the local player successfully extracts, enable freecam, notify other players about the extract
    /// </summary>
    /// <param name="player">The local player to start the Coroutine on</param>
    /// <param name="exfiltrationPoint">The exfiltration point that was used to extract</param>
    /// <param name="transitPoint">The transit point that was used to transit</param>
    /// <returns></returns>
    public void Extract(FikaPlayer player, ExfiltrationPoint exfiltrationPoint, TransitPoint transitPoint = null)
    {
        if (player.StatisticsManager is HealthStatisticsManager healthStatisticsManager)
        {
            healthStatisticsManager.ConsumeExperience();
        }
        GameController.Extract(player, exfiltrationPoint, transitPoint);
        MainPlayerExtracted?.Invoke(player);
    }

    /// <summary>
    /// Triggers when the main player dies
    /// </summary>
    /// <param name="damageType"></param>
    private async void HealthController_DiedEvent(EDamageType damageType)
    {
        try
        {
            await HandleDeath();
        }
        catch (Exception ex)
        {
            FikaGlobals.LogError($"HealthController_DiedEvent: {ex}");
        }
    }

    private async Task HandleDeath()
    {
        var player = _playerOwner.Player;
        if (player.QuestController is ClientSharedQuestController sharedQuestController)
        {
            sharedQuestController.ToggleQuestSharing(false);
        }
        if (GameController.TimeManager != null)
        {
            Destroy(GameController.TimeManager);
        }
        if (GameUi.TimerPanel != null && GameUi.TimerPanel.enabled)
        {
            GameUi.TimerPanel.Close();
        }

        player.HealthController.DiedEvent -= HealthController_DiedEvent;

        PlayerOwner.Stop();
        ExitStatus = ExitStatus.Killed;
        ExitLocation = string.Empty;

        if (FikaPlugin.Instance.Settings.ForceSaveOnDeath)
        {
            await SavePlayer((FikaPlayer)player, ExitStatus, string.Empty, true);
        }
    }

    /// <summary>
    /// Stops the local <see cref="CoopGame"/>
    /// </summary>
    public override void Stop(string profileId, ExitStatus exitStatus, string exitName, float delay = 0f)
    {
        FikaEventDispatcher.DispatchEvent(new FikaGameEndedEvent(GameController.IsServer, exitStatus, exitName));

        if (exitStatus < ExitStatus.Transit)
        {
            FikaBackendUtils.IsTransit = false;
        }

        if (FikaBackendUtils.IsTransit)
        {
            var data = FikaBackendUtils.TransitData;
            data.transitionType = ELocationTransition.Common;
            data.transitionCount++;
            data.visitedLocations = new Il2CppStringArray([.. data.visitedLocations ?? (IEnumerable<string>)[], Location.Id]);
            FikaBackendUtils.TransitData = data;
        }
        else
        {
            FikaBackendUtils.ResetTransitData();
        }

#if DEBUG
        _logger.LogDebug("Stop");
#endif

        ToggleDebug(false);

        GameController.DestroyDebugComponent();

        var myPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
        myPlayer.PacketSender.DestroyThis();

        if (myPlayer.Side != EPlayerSide.Savage && myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem != null)
        {
            var result = ItemManipulator.Remove(myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag)
                .ContainedItem, myPlayer.InventoryController, false);
            if (result.Error != null)
            {
                _logger.LogError("Error removing dog tag!");
            }
        }

        if (!myPlayer.ActiveHealthController.IsAlive && exitStatus == ExitStatus.Survived)
        {
            exitStatus = ExitStatus.Killed;
        }

        if (!ExtractedPlayers.Contains(myPlayer.NetId) && GameTimer.SessionTime.HasValue && GameTimer.PastTime >= GameTimer.SessionTime.Value)
        {
            exitStatus = ExitStatus.MissingInAction;
        }

        if (GameController.IsServer)
        {
            (GameController as HostGameController).StopBotsSystem(false);
        }

        var errors = 0;

        if (GameController.CoopHandler != null)
        {
            // Create a copy to prevent errors when the dictionary is being modified (which happens when using spawn mods)
            foreach (var player in (FikaPlayer[])[.. GameController.CoopHandler.Players.Values])
            {
                if (player == null || player.IsYourPlayer)
                {
                    continue;
                }

                // A transit keeps the handler alive and packets for these players still arrive until it is cleaned up
                GameController.CoopHandler.Players.Remove(player.NetId);
                GameController.CoopHandler.HumanPlayers.Remove(player);
                try
                {
                    player.Dispose();
                    AssetPoolObject.ReturnToPool(player.gameObject, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"There was an error disposing of player [{player.Profile.GetCorrectedNickname()}]: {ex}");
                    errors++;
                }
            }
        }
        else
        {
            _logger.LogError("Could not find CoopHandler!");
        }

        if (errors > 0)
        {
            NotificationManager.DisplayWarningNotification($"Failed to dispose of {errors} players/bots. Please check your log files and restart the client after this raid.");
            _logger.LogError($"Failed to dispose of {errors} players/bots.");
        }

        if (!FikaBackendUtils.IsTransit)
        {
            Destroy(GameController.CoopHandler);
        }

        ExitManager stopManager = new()
        {
            baseLocalGame_0 = this,
            profileId = profileId,
            exitStatus = exitStatus,
            exitName = exitName,
            delay = delay
        };

        var gameUI = GameUI.Instance;

        GameController.ExfilManager.Stop();

        Status = GameStatus.Stopping;
        GameTimer.TryStop();
        if (gameUI.TimerPanel.isActiveAndEnabled)
        {
            gameUI.TimerPanel.Close();
        }
        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.Stop();
        }
        MonoBehaviourSingleton<PreloaderUI>.Instance.StartBlackScreenShow(1f, 1f, new System.Action(stopManager.ExitOverride));
        AppEnvironment.Config.UseSpiritPlayer = false;
    }

    /// <summary>
    /// Saves your own <see cref="FikaPlayer"/> to the server
    /// </summary>
    private async Task SavePlayer(FikaPlayer player, ExitStatus exitStatus, string exitName, bool fromDeath)
    {
        if (_hasSaved)
        {
            return;
        }

        if (fromDeath)
        {
            //Since we're bypassing saving on exiting, run this now.
            player.Profile.EftStats.LastPlayerState = null;
            player.StatisticsManager.EndStatisticsSession(exitStatus, PastTime);
            player.CheckAndResetControllers(exitStatus, PastTime, Location.Id, exitName);
        }

        var playTimeDuration = DateTimeExtensions.Now - SessionStartTime;

        SessionResult parameters = new()
        {
            profile = new ProfileDescriptor(Profile, FikaGlobals.SearchControllerSerializer).ToUnparsedData(),
            result = exitStatus,
            killerId = _playerOwner.Player.KillerId,
            killerAid = _playerOwner.Player.KillerAccountId,
            exitName = exitName,
            inSession = true,
            favorite = Profile.Info.Side == EPlayerSide.Savage,
            playTime = (int)playTimeDuration.ToManaged().Duration().TotalSeconds,
            ProfileId = Profile.Id
        };

        try
        {
            await _backEnd.LocalRaidEnded(_raidSettings, parameters, GetLostInsuredItems(), GetOwnSentItems(player.ProfileId));
        }
        catch (Exception ex)
        {
            FikaGlobals.LogError("Exception caught when saving: " + ex.Message);
        }

        _hasSaved = true;
    }

    /// <summary>
    /// Retrieves the sent item containers associated with the specified profile from the player's own stash and transit
    /// stash, if available.
    /// </summary>
    /// <param name="profileId">The profile id to get the items from.</param>
    /// <returns>A dictionary mapping stash names to arrays of flat item data for each matching sent item container. The
    /// dictionary is empty if no matching containers are found.</returns>
    public Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppReferenceArray<FlatItem>> GetOwnSentItems(string profileId)
    {
        var instance = Singleton<GameWorld>.Instance;
        Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppReferenceArray<FlatItem>> dictionary = new();
        var btrController = instance.BtrController;
        if ((btrController?.TransferItemsController.Stash) != null)
        {
            var stash = btrController.TransferItemsController.Stash;
            var stashName = stash.Id + "_btr";
            foreach (var item in stash.Containers)
            {
                if (item.ID == profileId && !dictionary.ContainsKey(stashName))
                {
                    dictionary.Add(stashName, Singleton<ItemFactory>.Instance.TreeToFlatItems(item.Items));
                    break;
                }
            }

        }

        if (EFT.TransitController.Exist(out EFT.TransitController controller))
        {
            bool flag;
            if (controller == null)
            {
                flag = null != null;
            }
            else
            {
                var transferItemsController = controller.TransferItemsController;
                flag = (transferItemsController?.Stash) != null;
            }
            if (flag)
            {
                var stash2 = controller.TransferItemsController.Stash;
                var stashName = stash2.Id + "_transit";
                foreach (var item in stash2.Containers)
                {
                    if (item.ID == profileId && !dictionary.ContainsKey(stashName))
                    {
                        dictionary.Add(stashName, Singleton<ItemFactory>.Instance.TreeToFlatItems(item.Items));
                        break;
                    }
                }
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Stops the local <see cref="CoopGame"/> when waiting for other players
    /// </summary>
    public void StopFromCancel(string profileId, ExitStatus exitStatus)
    {
        if (exitStatus < ExitStatus.Transit)
        {
            FikaBackendUtils.IsTransit = false;
        }

        _logger.LogWarning("Game init was cancelled!");

        var myPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
        myPlayer.PacketSender.DestroyThis();

        if (myPlayer.Side != EPlayerSide.Savage)
        {
            if (myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem != null)
            {
                var result = ItemManipulator.Remove(myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem,
                    myPlayer.InventoryController, false);
                if (result.Error != null)
                {
                    _logger.LogWarning("StopFromError: Error removing dog tag!");
                }
            }
        }

        PlayerLeftRequest body = new(FikaBackendUtils.Profile.ProfileId);
        FikaRequestHandler.RaidLeave(body);

        if (GameController.CoopHandler != null)
        {
            foreach (var player in GameController.CoopHandler.Players.Values)
            {
                if (player == null)
                {
                    continue;
                }

                player.Dispose();
                AssetPoolObject.ReturnToPool(player.gameObject, true);
            }
            GameController.CoopHandler.Players.Clear();
        }
        else
        {
            _logger.LogError("Stop: Could not find CoopHandler!");
        }

        Destroy(GameController.CoopHandler);

        if (GameController.IsServer)
        {
            (GameController as HostGameController).StopBotsSystem(true);
        }


        const string exitName = null;
        const float delay = 0f;

        CancelExitManager stopManager = new()
        {
            baseLocalGame_0 = this,
            exitStatus = exitStatus,
            exitName = exitName,
            delay = delay
        };

        var gameUI = GameUI.Instance;

        if (GameController.ExfilManager != null)
        {
            GameController.ExfilManager.Stop();
        }

        Status = GameStatus.Stopping;
        if (GameTimer != null)
        {
            GameTimer.TryStop();
        }
        if (gameUI.TimerPanel.enabled)
        {
            gameUI.TimerPanel.Close();
        }

        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.Stop();
        }
        MonoBehaviourSingleton<PreloaderUI>.Instance.StartBlackScreenShow(1f, 1f, new System.Action(stopManager.ExitOverride));
        AppEnvironment.Config.UseSpiritPlayer = false;
    }

    /// <summary>
    /// Toggles the <see cref="DebugUI"/> menu
    /// </summary>
    public void ToggleDebug(bool enabled)
    {
        GameController.ToggleDebug(enabled);
    }

    /// <summary>
    /// Tells the server that we have left the raid
    /// </summary>
    public override ClientMetrics CollectMetrics()
    {
        if (!FikaBackendUtils.IsTransit)
        {
            try
            {
                PlayerLeftRequest body = new(FikaBackendUtils.Profile.ProfileId);
                FikaRequestHandler.RaidLeave(body);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to send RaidLeave request to server: " + ex.Message);
            }
        }
        _metricsCollector.Stop();
        return new();
    }

    /// <summary>
    /// Cleans up after the <see cref="CoopGame"/> stops
    /// </summary>
    public override void CleanUp()
    {
        foreach (var player in _players.Values)
        {
            try
            {
                if (player != null)
                {
                    player.Dispose();
                    AssetPoolObject.ReturnToPool(player.gameObject, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }
        _players.Clear();
        GameController.CleanUp();
        FikaBackendUtils.CleanUpVariables();
        BTRSide_Patches.Passengers.Clear();
    }

    private class ExitManager
    {
        public CoopGame baseLocalGame_0;
        public string profileId;
        public ExitStatus exitStatus;
        public string exitName;
        public float delay;

        public void ExitOverride()
        {
            baseLocalGame_0.GameUi.TimerPanel.Close();

            if (baseLocalGame_0._playerOwner != null)
            {
                baseLocalGame_0._playerOwner.Stop();
            }

            EftScreenManager.Instance.CloseAllScreensForced();

            //If we haven't saved, run the original method and stop running here.
            if (!baseLocalGame_0._hasSaved)
            {
                baseLocalGame_0._playerOwner.Player.TriggerZones.Clear();
                baseLocalGame_0._playerOwner.Player.TriggerZones.AddRange((baseLocalGame_0.GameController.LocalTriggerZones).ToIl2CppList());
                baseLocalGame_0.GameEnd(profileId, exitStatus, exitName, delay).HandleExceptions();
                return;
            }

            //Most of this is from GameEnd, minus the saving player part.
            _ = EndSession();
        }

        private async Task EndSession()
        {
            await baseLocalGame_0._playerOwner.Player.OnGameSessionEnd(exitStatus, baseLocalGame_0.PastTime,
                baseLocalGame_0.Location.Id, exitName).AsManaged();
            baseLocalGame_0.CleanUp();

            // The post raid screens wait on this flag, GameEnd sets it after saving and we bypassed GameEnd
            if (TarkovApplication.Exist(out var tarkovApplication))
            {
                tarkovApplication.GameOverSaveStatusReceived = true;
            }

            var game = baseLocalGame_0;
            var status = exitStatus;
            var duration = DateTimeExtensions.Now - game.SessionStartTime;
            StaticManager.Instance.WaitSeconds(delay, new Action(() =>
            {
                game._callback.Invoke(new Result<ExitStatus, Il2CppSystem.TimeSpan, ClientMetrics>(status, duration, game.CollectMetrics()));
                UIEventSystem.Instance.Enable();
                game._releaseProfilerSpikeAnalyser = null;
            }));
        }
    }

    /// <summary>
    /// Used to manage the stopping of the <see cref="CoopGame"/> gracefully when cancelling
    /// </summary>
    private class CancelExitManager
    {
        public CoopGame baseLocalGame_0;
        public string profileId;
        public ExitStatus exitStatus;
        public string exitName;
        public float delay;

        public void ExitOverride()
        {
            var instance = EftScreenManager.Instance;
            if (instance != null && instance.CheckCurrentScreen(EEftScreenType.Reconnect))
            {
                instance.CloseAllScreensForced();
            }
            if (baseLocalGame_0 != null)
            {
                baseLocalGame_0.CleanUp();
                baseLocalGame_0.Status = GameStatus.Stopped;
            }
            if (MonoBehaviourSingleton<BetterAudio>.Instance != null)
            {
                MonoBehaviourSingleton<BetterAudio>.Instance.FadeOutVolumeAfterRaid();
            }
            baseLocalGame_0.GameEnd(profileId, exitStatus, exitName, delay).HandleExceptions();
        }
    }

    public bool IsHeard()
    {
        if (Status != GameStatus.Started)
        {
            return false;
        }
        if (_localFikaPlayer == null)
        {
            return true;
        }
        var flag = VoiceClient.IsTalkDetected();
        _localFikaPlayer.TalkDateTime = (flag ? DateTimeExtensions.UtcNow : default).ToManaged();
        bool flag2;
        bool flag3;
        if (_players.Count == 1)
        {
            flag2 = true;
            flag3 = true;
        }
        else
        {
            flag2 = false;
            flag3 = false;
            var position = _localFikaPlayer.Position;
            foreach (var humanPlayer in GameController.CoopHandler.HumanPlayers)
            {
                if (humanPlayer.IsYourPlayer)
                {
                    continue;
                }

                var valueTuple = humanPlayer.IsHeard(ref position, _voipDistance);
                var item = valueTuple.Item1;
                var item2 = valueTuple.Item2;
                flag2 = flag2 || item;
                flag3 = flag3 || item2;
                if (flag2 && flag3)
                {
                    break;
                }
            }
        }
        VoiceClient.Blocked = !flag3;
        return flag2;
    }

    public void ReportAbuse()
    {
        _logger.LogInfo("NO");
    }
}
