using BepInEx.Logging;
using Comfort.Common;
using CommonAssets.Scripts.Game;
using Dissonance.Networking.Client;
using EFT;
using EFT.AssetsManager;
using EFT.Bots;
using EFT.CameraControl;
using EFT.EnvironmentEffect;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Screens;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.Patches;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.UI.Models;
using Fika.Core.Utils;
using HarmonyLib;
using JsonType;
using LiteNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.GameMode
{
    /// <summary>
    /// Coop game used in Fika
    /// </summary>
    public sealed class CoopGame : BaseLocalGame<EftGamePlayerOwner>, IFikaGame, IClientHearingTable
    {
        /*public static CoopGame Instance
        {
            get
            {
                if (localInstance != null)
                {
                    return localInstance;
                }

                if (Singleton<IFikaGame>.Instance is CoopGame coopGame)
                {
                    localInstance = coopGame;
                    return coopGame;
                }

                return null;
            }
            internal set
            {
                localInstance = value;
            }
        }*/

        private static CoopGame localInstance;

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

        public SeasonsSettingsClass SeasonsSettings
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

        private static ManualLogSource Logger;

        private Func<LocalPlayer, EftGamePlayerOwner> _func_1;
        private CoopPlayer _localPlayer;
        private bool _hasSaved;
        private float _voipDistance;

        /// <summary>
        /// Creates a CoopGame
        /// </summary>
        /// <param name="inputTree"></param>
        /// <param name="profile"></param>
        /// <param name="backendDateTime"></param>
        /// <param name="insurance"></param>
        /// <param name="menuUI"></param>
        /// <param name="gameUI"></param>
        /// <param name="location"></param>
        /// <param name="timeAndWeather"></param>
        /// <param name="wavesSettings"></param>
        /// <param name="dateTime"></param>
        /// <param name="callback"></param>
        /// <param name="fixedDeltaTime"></param>
        /// <param name="updateQueue"></param>
        /// <param name="backEndSession"></param>
        /// <param name="sessionTime"></param>
        /// <param name="raidSettings"></param>
        /// <returns></returns>
        internal static CoopGame Create(IInputTree inputTree, Profile profile, GameWorld gameWorld, GameDateTime backendDateTime,
            InsuranceCompanyClass insurance, GameUI gameUI, LocationSettingsClass.Location location,
            TimeAndWeatherSettings timeAndWeather, WavesSettings wavesSettings, EDateTime dateTime,
            Callback<ExitStatus, TimeSpan, MetricsClass> callback, float fixedDeltaTime, EUpdateQueue updateQueue,
            ISession backEndSession, TimeSpan sessionTime, MetricsEventsClass metricsEvents,
            MetricsCollectorClass metricsCollector, LocalRaidSettings localRaidSettings, RaidSettings raidSettings)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource("CoopGame");

            Singleton<IFikaNetworkManager>.Instance.RaidSide = profile.Side;

            GameDateTime gameTime = backendDateTime;
            if (timeAndWeather.HourOfDay != -1)
            {
                Logger.LogInfo($"Using custom time, hour of day: {timeAndWeather.HourOfDay}");
                DateTime currentTime = backendDateTime.DateTime_1;
                DateTime newTime = new(currentTime.Year, currentTime.Month, currentTime.Day, timeAndWeather.HourOfDay,
                    currentTime.Minute, currentTime.Second, currentTime.Millisecond);
                gameTime = new(backendDateTime.DateTime_0, newTime, backendDateTime.TimeFactor);
                gameTime.Reset(newTime);
                dateTime = EDateTime.CURR;
            }

            CoopGame coopGame = smethod_0<CoopGame>(inputTree, profile, gameWorld, gameTime, insurance, gameUI,
                location, timeAndWeather, wavesSettings, dateTime, callback, fixedDeltaTime, updateQueue, backEndSession,
                new TimeSpan?(sessionTime), metricsEvents, metricsCollector, localRaidSettings);
            coopGame.GameController = FikaBackendUtils.IsServer ? new HostGameController(coopGame, updateQueue, gameWorld, backEndSession, location, wavesSettings, coopGame.GameDateTime)
                : new ClientGameController(coopGame, updateQueue, gameWorld, backEndSession);
            coopGame.GameController.Location = location;

            float hearingDistance = FikaGlobals.VOIPHandler.PushToTalkSettings.HearingDistance;
            coopGame._voipDistance = hearingDistance * hearingDistance + 9;

            localInstance = coopGame;

            ClientHearingTable.Instance = coopGame;

            if (coopGame.GameController.IsServer)
            {
                gameWorld.World_0.method_0();
            }

            if (timeAndWeather.TimeFlowType != ETimeFlowType.x1)
            {
                float newFlow = timeAndWeather.TimeFlowType.ToTimeFlow();
                coopGame.GameWorld_0.GameDateTime.TimeFactor = newFlow;
                Logger.LogInfo($"Using custom time flow: {newFlow}");
            }

            if (OfflineRaidSettingsMenuPatch_Override.UseCustomWeather && coopGame.GameController.IsServer)
            {
                Logger.LogInfo("Custom weather enabled, initializing curves");
                (coopGame.GameController as HostGameController).SetupCustomWeather(timeAndWeather);
            }

            SetupGamePlayerOwnerHandler setupGamePlayerOwnerHandler = new(inputTree, insurance, backEndSession, gameUI, coopGame, location);
            coopGame._func_1 = setupGamePlayerOwnerHandler.HandleSetup;

            Singleton<IFikaGame>.Create(coopGame);
            FikaEventDispatcher.DispatchEvent(new FikaGameCreatedEvent(coopGame));

            EndByExitTrigerScenario endByExitTrigger = coopGame.GetComponent<EndByExitTrigerScenario>();
            EndByTimerScenario endByTimerScenario = coopGame.GetComponent<EndByTimerScenario>();

            if (endByExitTrigger != null)
            {
                Destroy(endByExitTrigger);
            }
            if (endByTimerScenario != null)
            {
                Destroy(endByTimerScenario);
            }

            coopGame.GameController.TimeManager = CoopTimeManager.Create(coopGame);
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
        private class SetupGamePlayerOwnerHandler(IInputTree inputTree, InsuranceCompanyClass insurance, ISession backEndSession, GameUI gameUI, CoopGame game, LocationSettingsClass.Location location)
        {
            private readonly IInputTree inputTree = inputTree;
            private readonly InsuranceCompanyClass insurance = insurance;
            private readonly ISession backEndSession = backEndSession;
            private readonly GameUI gameUI = gameUI;
            private readonly CoopGame game = game;
            private readonly LocationSettingsClass.Location location = location;

            public EftGamePlayerOwner HandleSetup(LocalPlayer player)
            {
                game.LocalPlayer_0 = player;
                EftGamePlayerOwner gamePlayerOwner = EftGamePlayerOwner.Create(player, inputTree, insurance, backEndSession, gameUI, game.GameDateTime, location);
                gamePlayerOwner.OnLeave += game.vmethod_4;
                return gamePlayerOwner;
            }
        }

        /*public override void vmethod_0()
        {
            localGameLoggerClass = new(LoggerMode.None, dictionary_0, GameController.Bots);
        }*/

        public override void SetMatchmakerStatus(string status, float? progress = null)
        {
            InvokeMatchingStatusChanged(status, progress);
        }

        /// <summary>
        /// The countdown deploy screen
        /// </summary>
        /// <returns></returns>
        public override IEnumerator vmethod_2()
        {
            yield return GameController.CountdownScreen(Profile_0, ProfileId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="layerName"></param>
        /// <param name="prefix"></param>
        /// <param name="pointOfView"></param>
        /// <param name="profile"></param>
        /// <param name="aiControl"></param>
        /// <param name="updateQueue"></param>
        /// <param name="armsUpdateMode"></param>
        /// <param name="bodyUpdateMode"></param>
        /// <param name="characterControllerMode"></param>
        /// <param name="getSensitivity"></param>
        /// <param name="getAimingSensitivity"></param>
        /// <param name="statisticsManager"></param>
        /// <param name="questController"></param>
        /// <param name="achievementsController"></param>
        /// <returns></returns>
        /// <exception cref="MissingComponentException"></exception>
        public override async Task<LocalPlayer> vmethod_3(GameWorld gameWorld, int playerId, Vector3 position, Quaternion rotation,
            string layerName, string prefix, EPointOfView pointOfView, Profile profile, bool aiControl,
            EUpdateQueue updateQueue, Player.EUpdateMode armsUpdateMode, Player.EUpdateMode bodyUpdateMode,
            CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity,
            Func<float> getAimingSensitivity, IStatisticsManager statisticsManager, ISession session,
            ELocalMode localMode)
        {
            if (!TransitControllerAbstractClass.IsTransit(profile.Id, out int _) && !FikaBackendUtils.IsReconnect)
            {
                profile.SetSpawnedInSession(false);
            }

            statisticsManager = FikaBackendUtils.IsHeadless ? new ObservedStatisticsManager() : new GClass2070();

            CoopPlayer coopPlayer = await CoopPlayer.Create(gameWorld, playerId, position, rotation, "Player", "Main_", EPointOfView.FirstPerson,
                profile, false, UpdateQueue, armsUpdateMode, Player.EUpdateMode.Auto,
                BackendConfigAbstractClass.Config.CharacterController.ClientPlayerMode, getSensitivity, getAimingSensitivity,
                statisticsManager, new GClass1660(), session, playerId);

            coopPlayer.Location = Location_0.Id;
            CoopHandler coopHandler = GameController.CoopHandler;

            if (coopHandler == null)
            {
                Logger.LogError("vmethod_3: CoopHandler was null!");
                throw new MissingComponentException("CoopHandler was missing during CoopGame init");
            }

            if (GameController.RaidSettings.MetabolismDisabled)
            {
                coopPlayer.HealthController.DisableMetabolism();
                NotificationManagerClass.DisplayMessageNotification(LocaleUtils.METABOLISM_DISABLED.Localized(), iconType: EFT.Communications.ENotificationIconType.Alert);
            }

            coopHandler.Players.Add(coopPlayer.NetId, coopPlayer);
            coopHandler.HumanPlayers.Add(coopPlayer);
            coopPlayer.SetupMainPlayer();

            PlayerSpawnRequest body = new(coopPlayer.ProfileId, FikaBackendUtils.GroupId);
            await FikaRequestHandler.UpdatePlayerSpawn(body);

            coopPlayer.SpawnPoint = GameController.SpawnPoint;

            //GameObject customButton = null;

            await NetManagerUtils.SetupGameVariables(coopPlayer);
            //customButton = CreateCancelButton(coopPlayer, customButton);

            if (!GameController.IsServer && !FikaBackendUtils.IsReconnect)
            {
                SendCharacterPacket packet = new(new()
                {
                    Profile = coopPlayer.Profile,
                    ControllerId = coopPlayer.InventoryController.CurrentId,
                    FirstOperationId = coopPlayer.InventoryController.NextOperationId
                }, coopPlayer.HealthController.IsAlive, false, coopPlayer.Transform.position, coopPlayer.NetId);
                FikaClient client = Singleton<FikaClient>.Instance;

                if (coopPlayer.ActiveHealthController != null)
                {
                    packet.PlayerInfoPacket.HealthByteArray = coopPlayer.ActiveHealthController.SerializeState();
                }

                if (coopPlayer.HandsController != null)
                {
                    packet.PlayerInfoPacket.ControllerType = HandsControllerToEnumClass.FromController(coopPlayer.HandsController);
                    packet.PlayerInfoPacket.ItemId = coopPlayer.HandsController.Item.Id;
                    packet.PlayerInfoPacket.IsStationary = coopPlayer.MovementContext.IsStationaryWeaponInHands;
                }

                client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }

            Logger.LogInfo("Adding debug component...");
            GameController.CreateDebugComponent();

            //Destroy(customButton);

            if (FikaBackendUtils.IsReconnect && !FikaBackendUtils.ReconnectPosition.Equals(Vector3.zero))
            {
                coopPlayer.Teleport(FikaBackendUtils.ReconnectPosition);
            }

            return coopPlayer;
        }

        /// <summary>
        /// This creates a "custom" Back button so that we can back out if we get stuck
        /// </summary>
        /// <param name="myPlayer"></param>
        /// <param name="coopPlayer"></param>
        /// <param name="customButton"></param>
        /// <returns></returns>
        private GameObject CreateCancelButton(LocalPlayer myPlayer, GameObject customButton)
        {
            if (myPlayer.Side is EPlayerSide.Savage)
            {
                return null;
            }

            if (MenuUI.Instantiated)
            {
                MenuUI menuUI = MenuUI.Instance;
                DefaultUIButton backButton = Traverse.Create(menuUI.MatchmakerTimeHasCome).Field<DefaultUIButton>("_cancelButton").Value;
                customButton = Instantiate(backButton.gameObject, backButton.gameObject.transform.parent);
                customButton.gameObject.name = "FikaBackButton";
                customButton.gameObject.SetActive(true);
                DefaultUIButton backButtonComponent = customButton.GetComponent<DefaultUIButton>();
                backButtonComponent.SetHeaderText("Cancel", 32);
                backButtonComponent.SetEnabledTooltip("EXPERIMENTAL: Cancels the matchmaking and returns to the menu.");
                UnityEngine.Events.UnityEvent newEvent = new();
                newEvent.AddListener(() =>
                {
                    GClass3595 errorScreen = Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("WARNING",
                        message: "Backing out from this stage is currently experimental. It is recommended to ALT+F4 instead. Do you still want to continue?",
                        ErrorScreen.EButtonType.OkButton, 15f);
                    errorScreen.OnAccept += StopFromCancel(myPlayer);
                });
                Traverse.Create(backButtonComponent).Field("OnClick").SetValue(newEvent);
            }

            return customButton;
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
        /// <param name="botsSettings"></param>
        /// <param name="backendUrl"></param>
        /// <param name="runCallback"></param>
        /// <returns></returns>
        public async Task InitPlayer(BotControllerSettings botsSettings, string backendUrl)
        {
            if (FikaBackendUtils.IsHeadless)
            {
                Logger.LogWarning("Unloading resources");
                await Resources.UnloadUnusedAssets().Await();
            }

            Status = GameStatus.Running;
            UnityEngine.Random.InitState((int)EFTDateTimeClass.Now.Ticks);

            if (!GameController.IsServer)
            {
                await (GameController as ClientGameController).WaitForHostToLoad();
            }

            await GameController.SetupCoopHandler(this);

            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            gameWorld.LocationId = Location_0.Id;

            ExfiltrationControllerClass.Instance.InitAllExfiltrationPoints(Location_0._Id, Location_0.exits, Location_0.SecretExits,
                !GameController.IsServer, Location_0.DisabledScavExits);

            Logger.LogInfo($"Location: {Location_0.Name}");
            BackendConfigSettingsClass instance = Singleton<BackendConfigSettingsClass>.Instance;

            GameController.InitShellingController(instance, gameWorld, Location_0);
            GameController.InitHalloweenEvent(instance, gameWorld, Location_0);
            GameController.InitBTRController(instance, gameWorld, Location_0);

            if ((FikaBackendUtils.IsHeadless || FikaBackendUtils.IsHeadlessGame) && FikaPlugin.Instance.EnableTransits)
            {
                GameController.InitializeTransitSystem(gameWorld, instance, Profile_0, localRaidSettings_0, Location_0);
            }

            GameController.InitializeRunddans(instance, gameWorld, Location_0);

            gameWorld.ClientBroadcastSyncController = new ClientBroadcastSyncControllerClass();

            ApplicationConfigClass config = BackendConfigAbstractClass.Config;
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
                LocalPlayer player = await CreateLocalPlayer() ?? throw new NullReferenceException("InitPlayer: Player was null!");
                dictionary_0.Add(player.ProfileId, player);
                gparam_0 = _func_1(player);
                PlayerCameraController.Create(gparam_0.Player);
                CameraClass.Instance.SetOcclusionCullingEnabled(Location_0.OcculsionCullingEnabled);
                CameraClass.Instance.IsActive = false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"InitPlayer: {ex.Message}");
                throw;
            }

            await GameController.WaitForHostToStart();

            LocationSettingsClass.Location location = localRaidSettings_0.selectedLocation;

            await GameController.InitializeLoot(Location_0);
            await method_11(location);

            GameController.CoopHandler.ShouldSync = true;

            if (FikaBackendUtils.IsReconnect)
            {
                await Reconnect();
                foreach (KeyValuePair<EBodyPart, GClass2852<ActiveHealthController.GClass2851>.BodyPartState> item in gparam_0.Player.ActiveHealthController.Dictionary_0)
                {
                    if (item.Value.Health.AtMinimum)
                    {
                        item.Value.IsDestroyed = true;
                    }
                }
            }

            await vmethod_1(botsSettings, null);

            if (GameController.IsServer)
            {
                Singleton<IBotGame>.Instance.BotsController.CoversData.Patrols.RestoreLoot(Location_0.Loot, LocationScene.GetAllObjects<LootableContainer>(false));
                AirdropEventClass airdropEventClass = new()
                {
                    AirdropParameters = Location_0.airdropParameters
                };
                airdropEventClass.Init(true);
                (Singleton<GameWorld>.Instance as ClientGameWorld).ClientSynchronizableObjectLogicProcessor.ServerAirdropManager = airdropEventClass;
                GameWorld_0.SynchronizableObjectLogicProcessor.Ginterface257_0 = Singleton<FikaServer>.Instance;
            }

            await method_6();
            FikaEventDispatcher.DispatchEvent(new GameWorldStartedEvent(GameWorld_0));
        }

        private async Task GetReconnectProfile(string profileId)
        {
            Profile_0 = null;

            ReconnectPacket reconnectPacket = new()
            {
                IsRequest = true,
                InitialRequest = true,
                ProfileId = profileId
            };
            FikaClient client = Singleton<FikaClient>.Instance;
            client.SendData(ref reconnectPacket, DeliveryMethod.ReliableOrdered);

            do
            {
                await Task.Delay(250);
            } while (Profile_0 == null);

            await Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(PoolManagerClass.PoolsCategory.Raid, PoolManagerClass.AssemblyType.Local,
                Profile_0.GetAllPrefabPaths(true).ToArray(), JobPriorityClass.General);
        }

        private async Task Reconnect()
        {
            SetMatchmakerStatus(LocaleUtils.UI_RECONNECTING.Localized());

            ReconnectPacket reconnectPacket = new()
            {
                IsRequest = true,
                ProfileId = ProfileId
            };
            FikaClient client = Singleton<FikaClient>.Instance;
            client.SendData(ref reconnectPacket, DeliveryMethod.ReliableOrdered);

            do
            {
                await Task.Delay(1000);
            } while (!client.ReconnectDone);
        }



        /// <summary>
        /// Creates the local player
        /// </summary>
        /// <returns>A <see cref="Player"/></returns>
        private async Task<LocalPlayer> CreateLocalPlayer()
        {
            Status = GameStatus.Running;

            int num = Singleton<IFikaNetworkManager>.Instance.NetId;

            Player.EUpdateMode eupdateMode = Player.EUpdateMode.Auto;
            if (BackendConfigAbstractClass.Config.UseHandsFastAnimator)
            {
                eupdateMode = Player.EUpdateMode.Manual;
            }

            if (GameController.IsServer)
            {
                GameController.CreateSpawnSystem(Profile_0);
            }
            else
            {
                await GameController.ReceiveSpawnPoint(Profile_0);
                if (string.IsNullOrEmpty(GameController.InfiltrationPoint))
                {
                    Logger.LogError("InfiltrationPoint was null after retrieving it from the server!");
                    GameController.CreateSpawnSystem(Profile_0);
                }

                ClientGameController clientGameController = (ClientGameController)GameController;
                await clientGameController.InitInteractables();
                await clientGameController.InitExfils();
            }

            GameController.ExfilManager = gameObject.AddComponent<CoopExfilManager>();

            if (Location_0.AccessKeys != null && Location_0.AccessKeys.Length > 0)
            {
                IEnumerable<Item> items = Profile_0.Inventory.GetPlayerItems(EPlayerItems.Equipment);
                if (items != null)
                {
                    Class1520 keyFinder = new()
                    {
                        accessKeys = Location_0.AccessKeys
                    };
                    Item accessKey = items.FirstOrDefault(keyFinder.method_0);
                    if (accessKey != null)
                    {
                        method_5(Profile_0, accessKey.Id);
                    }
                }
            }

            if (Singleton<IFikaNetworkManager>.Instance.AllowVOIP)
            {
                Logger.LogInfo("VOIP enabled, initializing...");
                try
                {
                    await Singleton<IFikaNetworkManager>.Instance.InitializeVOIP();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"There was an error initializing the VOIP module: {ex.Message}");
                }
            }

            IStatisticsManager statisticsManager = new CoopClientStatisticsManager(Profile_0);

            Vector3 spawnPos = GameController.GetSpawnPosition();
            Quaternion spawnRot = GameController.GetSpawnRotation();

            LocalPlayer myPlayer;
            try
            {
                if (Profile_0.Side != EPlayerSide.Savage)
                {
                    GenerateNewDogTagId();
                }
                else if (FikaBackendUtils.IsHeadless)
                {
                    Profile_0.Info.Nickname = Profile_0.Info.Nickname.Insert(0, "headless_");
                }

                myPlayer = await vmethod_3(GameWorld_0, num, spawnPos, spawnRot, "Player", "", EPointOfView.FirstPerson,
                        Profile_0, false, UpdateQueue, eupdateMode, Player.EUpdateMode.Auto,
                        BackendConfigAbstractClass.Config.CharacterController.ClientPlayerMode,
                        FikaGlobals.GetLocalPlayerSensitivity, FikaGlobals.GetLocalPlayerAimingSensitivity, statisticsManager,
                        iSession, (localRaidSettings_0 != null) ? localRaidSettings_0.mode : ELocalMode.TRAINING);
            }
            catch (Exception ex)
            {
                Logger.LogError($"CreateLocalPlayer: {ex.Message}");
                throw;
            }

            myPlayer.OnEpInteraction += OnEpInteraction;

            _localPlayer = myPlayer as CoopPlayer;
            GameController.SetLocalPlayer(_localPlayer);

            Logger.LogInfo("Local player created");
            return myPlayer;
        }

        /// <summary>
        /// Temporary workaround since SPT does not generate a unique ID >3.11 for the dogtag
        /// </summary>
        private void GenerateNewDogTagId()
        {
            Item dogTag = Profile_0.Inventory.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem;
            if (dogTag != null)
            {
                Traverse.Create(dogTag).Field<string>("<Id>k__BackingField").Value = MongoID.Generate(true);
#if DEBUG
                Logger.LogWarning("Generated new ID for DogTag");
#endif
            }
            else
            {
                Logger.LogError("Could not find DogTag when generating new ID!");
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
            Logger.LogInfo("Setting game status to: " + status.ToString());
        }

        /// <summary>
        /// Bot System Starter -> Countdown
        /// </summary>
        /// <param name="controllerSettings"></param>
        /// <param name="spawnSystem"></param>
        /// <returns></returns>
        public override async Task vmethod_1(BotControllerSettings controllerSettings, ISpawnSystem spawnSystem)
        {
            await GameController.StartBotSystemsAndCountdown(controllerSettings, GameWorld_0);
        }

        public override IEnumerator vmethod_5(Action runCallback)
        {
            yield return GameController.FinishRaidSetup();
            yield return base.vmethod_5(runCallback);
        }

        public override void Spawn()
        {
            if (LocalPlayer_0.ActiveHealthController is CoopClientHealthController coopClientHealthController)
            {
                coopClientHealthController.Start();
            }
            gparam_0.Player.HealthController.DiedEvent += HealthController_DiedEvent;
            gparam_0.vmethod_0();
        }

        /// <summary>
        /// Sets up <see cref="HealthControllerClass"/> events and all <see cref="ExfiltrationPoint"/>s
        /// </summary>
        public override void vmethod_6()
        {
            GameController.SetupEventsAndExfils(gparam_0.Player);
            dateTime_0 = EFTDateTimeClass.Now;
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

            Logger.LogError("CoopGame::UpdateExfilPointFromServer: ExfilManager was null!");
        }

        public override void Dispose()
        {
            ClientHearingTable.Instance = null;
            foreach (Player player in dictionary_0.Values)
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
                    Logger.LogError(ex);
                }
            }
            dictionary_0.Clear();
            base.Dispose();
        }

        /// <summary>
        /// When the local player successfully extracts, enable freecam, notify other players about the extract
        /// </summary>
        /// <param name="player">The local player to start the Coroutine on</param>
        /// <param name="exfiltrationPoint">The exfiltration point that was used to extract</param>
        /// <param name="transitPoint">The transit point that was used to transit</param>
        /// <returns></returns>
        public void Extract(CoopPlayer player, ExfiltrationPoint exfiltrationPoint, TransitPoint transitPoint = null)
        {
            GameController.Extract(player, exfiltrationPoint, transitPoint);
        }

        /// <summary>
        /// Triggers when the main player dies
        /// </summary>
        /// <param name="damageType"></param>
        private async void HealthController_DiedEvent(EDamageType damageType)
        {
            Player player = gparam_0.Player;
            if (player.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
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

            player.HealthController.DiedEvent -= method_18;
            player.HealthController.DiedEvent -= HealthController_DiedEvent;

            PlayerOwner.vmethod_1();
            ExitStatus = ExitStatus.Killed;
            ExitLocation = string.Empty;

            if (FikaPlugin.Instance.ForceSaveOnDeath)
            {
                await SavePlayer((CoopPlayer)player, ExitStatus, string.Empty, true);
            }
        }

        /// <summary>
        /// Stops the local <see cref="CoopGame"/>
        /// </summary>
        /// <param name="profileId"></param>
        /// <param name="exitStatus"></param>
        /// <param name="exitName"></param>
        /// <param name="delay"></param>
        public override void Stop(string profileId, ExitStatus exitStatus, string exitName, float delay = 0f)
        {
            FikaEventDispatcher.DispatchEvent(new FikaGameEndedEvent(GameController.IsServer, exitStatus, exitName));

            if (exitStatus < ExitStatus.Transit)
            {
                FikaBackendUtils.IsTransit = false;
            }

            if (FikaBackendUtils.IsTransit)
            {
                RaidTransitionInfoClass data = FikaBackendUtils.TransitData;
                data.transitionType = ELocationTransition.Common;
                data.transitionCount++;
                data.visitedLocations = [.. data.visitedLocations, Location_0.Id];
                FikaBackendUtils.TransitData = data;
            }
            else
            {
                FikaBackendUtils.ResetTransitData();
            }

            Logger.LogDebug("Stop");

            ToggleDebug(false);

            GameController.DestroyDebugComponent();

            CoopPlayer myPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            myPlayer.PacketSender.DestroyThis();

            if (myPlayer.Side != EPlayerSide.Savage)
            {
                if (myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem != null)
                {
                    GStruct440<GClass3249> result = InteractionsHandlerClass.Remove(myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem,
                        myPlayer.InventoryController, false);
                    if (result.Error != null)
                    {
                        Logger.LogError("Stop: Error removing dog tag!");
                    }
                }
            }

            if (!myPlayer.ActiveHealthController.IsAlive && exitStatus == ExitStatus.Survived)
            {
                exitStatus = ExitStatus.Killed;
            }

            if (!ExtractedPlayers.Contains(myPlayer.NetId))
            {
                if (GameTimer.SessionTime != null && GameTimer.PastTime >= GameTimer.SessionTime)
                {
                    exitStatus = ExitStatus.MissingInAction;
                }
            }

            if (GameController.IsServer)
            {
                (GameController as HostGameController).StopBotsSystem(false);
            }

            if (GameController.CoopHandler != null)
            {
                // Create a copy to prevent errors when the dictionary is being modified (which happens when using spawn mods)
                CoopPlayer[] players = [.. GameController.CoopHandler.Players.Values];
                foreach (CoopPlayer player in players)
                {
                    if (player == null || player.IsYourPlayer)
                    {
                        continue;
                    }

                    player.Dispose();
                    AssetPoolObject.ReturnToPool(player.gameObject, true);
                }
            }
            else
            {
                Logger.LogError("Stop: Could not find CoopHandler!");
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

            GameUI gameUI = GameUI.Instance;

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
            MonoBehaviourSingleton<PreloaderUI>.Instance.StartBlackScreenShow(1f, 1f, stopManager.ExitOverride);
            BackendConfigAbstractClass.Config.UseSpiritPlayer = false;
        }

        /// <summary>
        /// Saves your own <see cref="CoopPlayer"/> to the server
        /// </summary>
        /// <param name="player"></param>
        /// <param name="exitStatus"></param>
        /// <param name="exitName"></param>
        /// <param name="fromDeath"></param>
        /// <returns></returns>
        private async Task SavePlayer(CoopPlayer player, ExitStatus exitStatus, string exitName, bool fromDeath)
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
                player.CheckAndResetControllers(exitStatus, PastTime, Location_0.Id, exitName);
            }

            TimeSpan playTimeDuration = EFTDateTimeClass.Now - dateTime_0;

            RaidEndDescriptorClass parameters = new()
            {
                profile = new CompleteProfileDescriptorClass(Profile_0, FikaGlobals.SearchControllerSerializer).ToUnparsedData(),
                result = exitStatus,
                killerId = gparam_0.Player.KillerId,
                killerAid = gparam_0.Player.KillerAccountId,
                exitName = exitName,
                inSession = true,
                favorite = Profile_0.Info.Side == EPlayerSide.Savage,
                playTime = (int)playTimeDuration.Duration().TotalSeconds,
                ProfileId = Profile_0.Id
            };

            try
            {
                await iSession.LocalRaidEnded(localRaidSettings_0, parameters, method_12(), GetOwnSentItems(player.ProfileId));
            }
            catch (Exception ex)
            {
                FikaPlugin.Instance.FikaLogger.LogError("Exception caught when saving: " + ex.Message);
            }

            _hasSaved = true;
        }

        public Dictionary<string, FlatItemsDataClass[]> GetOwnSentItems(string profileId)
        {
            GameWorld instance = Singleton<GameWorld>.Instance;
            Dictionary<string, FlatItemsDataClass[]> dictionary = [];
            BTRControllerClass btrController = instance.BtrController;
            if ((btrController?.TransferItemsController.Stash) != null)
            {
                StashItemClass stash = btrController.TransferItemsController.Stash;
                string stashName = stash.Id + "_btr";
                foreach (EFT.InventoryLogic.IContainer item in stash.Containers)
                {
                    if (item.ID == profileId && !dictionary.ContainsKey(stashName))
                    {
                        dictionary.Add(stashName, Singleton<ItemFactoryClass>.Instance.TreeToFlatItems(item.Items));
                        break;
                    }
                }

            }

            if (TransitControllerAbstractClass.Exist(out TransitControllerAbstractClass controller))
            {
                bool flag;
                if (controller == null)
                {
                    flag = null != null;
                }
                else
                {
                    TransferItemsControllerAbstractClass transferItemsController = controller.TransferItemsController;
                    flag = (transferItemsController?.Stash) != null;
                }
                if (flag)
                {
                    StashItemClass stash2 = controller.TransferItemsController.Stash;
                    string stashName = stash2.Id + "_transit";
                    foreach (EFT.InventoryLogic.IContainer item in stash2.Containers)
                    {
                        if (item.ID == profileId && !dictionary.ContainsKey(stashName))
                        {
                            dictionary.Add(stashName, Singleton<ItemFactoryClass>.Instance.TreeToFlatItems(item.Items));
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
        /// <param name="profileId"></param>
        /// <param name="exitStatus"></param>
        public void StopFromCancel(string profileId, ExitStatus exitStatus)
        {
            if (exitStatus < ExitStatus.Transit)
            {
                FikaBackendUtils.IsTransit = false;
            }

            Logger.LogWarning("Game init was cancelled!");

            CoopPlayer myPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            myPlayer.PacketSender.DestroyThis();

            if (myPlayer.Side != EPlayerSide.Savage)
            {
                if (myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem != null)
                {
                    GStruct440<GClass3249> result = InteractionsHandlerClass.Remove(myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem,
                        myPlayer.InventoryController, false);
                    if (result.Error != null)
                    {
                        Logger.LogWarning("StopFromError: Error removing dog tag!");
                    }
                }
            }

            string exitName = null;
            float delay = 0f;

            PlayerLeftRequest body = new(FikaBackendUtils.Profile.ProfileId);
            FikaRequestHandler.RaidLeave(body);

            if (GameController.CoopHandler != null)
            {
                foreach (CoopPlayer player in GameController.CoopHandler.Players.Values)
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
                Logger.LogError("Stop: Could not find CoopHandler!");
            }

            Destroy(GameController.CoopHandler);

            if (GameController.IsServer)
            {
                (GameController as HostGameController).StopBotsSystem(true);
            }

            CancelExitManager stopManager = new()
            {
                baseLocalGame_0 = this,
                exitStatus = exitStatus,
                exitName = exitName,
                delay = delay
            };

            GameUI gameUI = GameUI.Instance;

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
            MonoBehaviourSingleton<PreloaderUI>.Instance.StartBlackScreenShow(1f, 1f, stopManager.ExitOverride);
            BackendConfigAbstractClass.Config.UseSpiritPlayer = false;
        }

        /// <summary>
        /// Toggles the <see cref="FikaDebug"/> menu
        /// </summary>
        /// <param name="enabled"></param>
        public void ToggleDebug(bool enabled)
        {
            GameController.ToggleDebug(enabled);
        }

        /// <summary>
        /// Tells the server that we have left the raid and 
        /// </summary>
        /// <returns></returns>
        public override MetricsClass vmethod_7()
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
                    Logger.LogError("Unable to send RaidLeave request to server: " + ex.Message);
                }
            }
            metricsCollectorClass.Stop();
            return new();
        }

        /// <summary>
        /// Cleans up after the <see cref="CoopGame"/> stops
        /// </summary>
        public override void CleanUp()
        {
            foreach (Player player in dictionary_0.Values)
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
                    Logger.LogError(ex);
                }
            }
            dictionary_0.Clear();
            GameController.CleanUp();
            FikaBackendUtils.CleanUpVariables();
            BTRSide_Patches.Passengers.Clear();
        }

        private class ExitManager : Class1522
        {
            public new CoopGame baseLocalGame_0;

            public void ExitOverride()
            {
                baseLocalGame_0.GameUi.TimerPanel.Close();

                if (baseLocalGame_0.gparam_0 != null)
                {
                    baseLocalGame_0.gparam_0.vmethod_1();
                }

                CurrentScreenSingletonClass.Instance.CloseAllScreensForced();

                //If we haven't saved, run the original method and stop running here.
                if (!baseLocalGame_0._hasSaved)
                {
                    baseLocalGame_0.gparam_0.Player.TriggerZones.Clear();
                    foreach (string triggerZone in baseLocalGame_0.GameController.LocalTriggerZones)
                    {
                        baseLocalGame_0.gparam_0.Player.TriggerZones.Add(triggerZone);
                    }
                    baseLocalGame_0.method_14(profileId, exitStatus, exitName, delay).HandleExceptions();
                    return;
                }

                //Most of this is from method_14, minus the saving player part.
                baseLocalGame_0.gparam_0.Player.OnGameSessionEnd(exitStatus, baseLocalGame_0.PastTime, baseLocalGame_0.Location_0.Id, exitName);
                baseLocalGame_0.CleanUp();

                Class1523 exitCallback = new()
                {
                    baseLocalGame_0 = baseLocalGame_0,
                    duration = EFTDateTimeClass.Now - baseLocalGame_0.dateTime_0,
                    exitStatus = exitStatus
                };

                StaticManager.Instance.WaitSeconds(delay, exitCallback.method_0);
            }
        }

        /// <summary>
        /// Used to manage the stopping of the <see cref="CoopGame"/> gracefully when cancelling
        /// </summary>
        private class CancelExitManager : Class1522
        {
            public void ExitOverride()
            {
                CurrentScreenSingletonClass instance = CurrentScreenSingletonClass.Instance;
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
                baseLocalGame_0.method_14(profileId, exitStatus, exitName, delay).HandleExceptions();
            }
        }        

        public bool IsHeard()
        {
            if (Status != GameStatus.Started)
            {
                return false;
            }
            if (_localPlayer == null)
            {
                return true;
            }
            bool flag = GClass1244.IsTalkDetected();
            _localPlayer.TalkDateTime = flag ? EFTDateTimeClass.UtcNow : default;
            bool flag2;
            bool flag3;
            if (dictionary_0.Count == 1)
            {
                flag2 = true;
                flag3 = true;
            }
            else
            {
                flag2 = false;
                flag3 = false;
                Vector3 position = _localPlayer.Position;
                foreach (CoopPlayer humanPlayer in GameController.CoopHandler.HumanPlayers)
                {
                    if (humanPlayer.IsYourPlayer)
                    {
                        continue;
                    }

                    ValueTuple<bool, bool> valueTuple = humanPlayer.IsHeard(in position, _voipDistance);
                    bool item = valueTuple.Item1;
                    bool item2 = valueTuple.Item2;
                    flag2 = flag2 || item;
                    flag3 = flag3 || item2;
                    if (flag2 && flag3)
                    {
                        break;
                    }
                }
            }
            GClass1244.Blocked = !flag3;
            return flag2;
        }

        public void ReportAbuse()
        {
            Logger.LogInfo("NO");
        }
    }
}
