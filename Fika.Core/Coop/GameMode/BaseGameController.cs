using Audio.AmbientSubsystem;
using BepInEx.Logging;
using Comfort.Common;
using CommonAssets.Scripts.Audio.RadioSystem;
using Dissonance;
using EFT;
using EFT.Game.Spawning;
using EFT.GameTriggers;
using EFT.GlobalEvents;
using EFT.Interactive;
using EFT.UI;
using EFT.UI.Matchmaker;
using EFT.UI.Screens;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.HostClasses;
using Fika.Core.Coop.Patches;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Utils;
using HarmonyLib;
using LiteNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Fika.Core.Networking.GenericSubPackets;
using static Fika.Core.Networking.SubPacket;
using static LocationSettingsClass;

namespace Fika.Core.Coop.GameMode
{
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

            Logger = new(GetType().Name);
        }

        public ManualLogSource Logger { get; set; }

        protected IFikaGame _fikaGame;
        protected AbstractGame _abstractGame;

        public bool IsServer { get; private set; }
        public bool RaidStarted { get; internal set; }
        public CoopTimeManager TimeManager { get; set; }

        // Weather
        public ESeason Season
        {
            get
            {
                return season;
            }
            set
            {
                season = value;
                Logger.LogInfo($"Setting Season to: {value}");
                WeatherReady = true;
            }
        }
        public bool WeatherReady { get; internal set; }
        public WeatherClass[] WeatherClasses { get; set; }
        public SeasonsSettingsClass SeasonsSettings { get; set; }
        public CoopExfilManager ExfilManager { get; set; }

        // Raid data
        public List<ThrowWeapItemClass> ThrownGrenades { get; set; }
        public RaidSettings RaidSettings { get; set; }
        public GClass1370 LootItems { get; set; } = [];
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
        protected SpawnPointManagerClass _spawnPoints;
        protected ISpawnPoint _spawnPoint;        

        private CoopHalloweenEventManager _halloweenEventManager;
        private FikaDebug _fikaDebug;

        private ESeason season;

        protected CoopHandler _coopHandler;
        protected CoopPlayer _localPlayer;
        protected EUpdateQueue _updateQueue;
        protected GameWorld _gameWorld;
        protected ISession _backendSession;
        protected Coroutine _extractRoutine;

        public void SetLocalPlayer(CoopPlayer player)
        {
            _localPlayer = player;
            _coopHandler.MyPlayer = player;
        }

        public virtual IEnumerator WaitForHostInit(int timeBeforeDeployLocal)
        {
            throw new NotImplementedException("Use derived classes");
        }

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
            _fikaDebug = _abstractGame.gameObject.AddComponent<FikaDebug>();
        }

        public Task SetupCoopHandler(CoopGame coopGame = null)
        {
            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                _coopHandler = coopHandler;
                if (coopGame != null)
                {
                    _coopHandler.LocalGameInstance = coopGame;
                }
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

        public async Task CreateStashes()
        {
            if (_gameWorld.TransitController != null)
            {
                while (_gameWorld.TransitController.TransferItemsController == null)
                {
                    await Task.Delay(100);
                }

                while (_gameWorld.TransitController.TransferItemsController.Stash == null)
                {
                    await Task.Delay(100);
                }
            }

            if (_gameWorld.BtrController != null)
            {
                while (_gameWorld.BtrController.TransferItemsController == null)
                {
                    await Task.Delay(100);
                }

                while (_gameWorld.BtrController.TransferItemsController.Stash == null)
                {
                    await Task.Delay(100);
                }
            }

            if (_coopHandler != null)
            {
                for (int i = 0; i < _coopHandler.HumanPlayers.Count; i++)
                {
                    CoopPlayer player = _coopHandler.HumanPlayers[i];
                    try
                    {
                        if (_gameWorld.TransitController != null)
                        {
                            _gameWorld.TransitController.TransferItemsController.InitPlayerStash(player);
                        }

                        if (_gameWorld.BtrController != null)
                        {
                            _gameWorld.BtrController.TransferItemsController.InitPlayerStash(player);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Could not initialize transfer stash on {player.Profile.Nickname}: {ex.Message}");
                    }
                }
            }
            else
            {
                Logger.LogError("Could not find CoopHandler when trying to initialize player stashes for TransferItemsController!");
            }
        }

        /// <summary>
        /// This task ensures that all players are joined and loaded before continuing
        /// </summary>
        /// <returns></returns>
        public virtual async Task WaitForOtherPlayersToLoad()
        {
            await Task.Yield();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a few last changes to the raid setup
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator FinishRaidSetup()
        {
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_FINISHING_RAID_INIT.Localized());

            WaitForEndOfFrame endOfFrame = new();
            Task musicTask = Singleton<GUISounds>.Instance.method_10(false, CancellationToken.None);
            while (!musicTask.IsCompleted)
            {
                yield return endOfFrame;
            }

            GClass2114.ResetAudioBuffer();

            _gameWorld.TriggersModule = _abstractGame.gameObject.AddComponent<LocalClientTriggersModule>();
            _gameWorld.FillLampControllers();
            if (Location.Id == "laboratory")
            {
                Season = ESeason.Summer;
            }
            WeatherReady = true;
            OfflineRaidSettingsMenuPatch_Override.UseCustomWeather = false;

            Class438 seasonController = new();
            _gameWorld.GInterface29_0 = seasonController;

#if DEBUG
            Logger.LogWarning($"Running season handler for season: {Season}");
#endif
            Task runSeason = seasonController.Run(Season, SeasonsSettings);
            while (!runSeason.IsCompleted)
            {
                yield return endOfFrame;
            }

            if (MonoBehaviourSingleton<RadioBroadcastController>.Instantiated)
            {
                MonoBehaviourSingleton<RadioBroadcastController>.Instance.StartBroadcast();
            }
        }

        public virtual async Task GenerateWeathers()
        {
            await Task.Yield();
            throw new NotImplementedException();
        }

        public virtual IEnumerator CountdownScreen(Profile profile, string profileId)
        {
            FikaBackendUtils.GroupPlayers.Clear();

            int timeBeforeDeployLocal = FikaBackendUtils.IsReconnect ? 3 : Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal;
#if DEBUG
            timeBeforeDeployLocal = 3;
#endif
            yield return WaitForHostInit(timeBeforeDeployLocal);

            DateTime dateTime = EFTDateTimeClass.Now.AddSeconds(timeBeforeDeployLocal);
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
            _ = Task.Run(CreateStashes);

            if (FikaPlugin.UseFikaGC.Value)
            {
                NetManagerUtils.FikaGameObject.AddComponent<GCManager>();
            }
        }

        private IEnumerator FixVOIPAudioDevice()
        {
            // Todo: Find root causes and fix elegantly...
            DissonanceComms.Instance.IsMuted = false;
            yield return new WaitForSeconds(1);
            DissonanceComms.Instance.IsMuted = true;

            for (int i = 0; i < _coopHandler.HumanPlayers.Count; i++)
            {
                CoopPlayer player = _coopHandler.HumanPlayers[i];
                if (player.IsYourPlayer || player.Profile.IsHeadlessProfile())
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

            for (int i = 0; i < _coopHandler.HumanPlayers.Count; i++)
            {
                CoopPlayer player = _coopHandler.HumanPlayers[i];
                if (player.IsYourPlayer || player.Profile.IsHeadlessProfile())
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
            TransitControllerAbstractClass transitController = Singleton<GameWorld>.Instance.TransitController;
            if (transitController == null)
            {
                if (FikaPlugin.Instance.EnableTransits)
                {
                    Logger.LogError("SyncTransitControllers: TransitController was null!");
                }
                return;
            }

            if (transitController.summonedTransits.TryGetValue(profileId, out TransitDataClass transitData))
            {
                SyncTransitControllersPacket packet = new()
                {
                    ProfileId = profileId,
                    RaidId = transitData.raidId,
                    Count = transitData.count,
                    Maps = transitData.maps
                };

                if (IsServer)
                {
                    Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
                    return;
                }

                Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
                return;
            }

            Logger.LogError("SyncTransitControllers: Could not find TransitData in Summonedtransits!");
        }

        public virtual async Task ReceiveSpawnPoint(Profile profile)
        {
            await Task.Yield();
            throw new NotImplementedException();
        }

        public virtual void CreateSpawnSystem(Profile profile)
        {
            throw new NotImplementedException();
        }

        public void InitShellingController(BackendConfigSettingsClass instance, GameWorld gameWorld, LocationSettingsClass.Location location)
        {
            if (instance != null && instance.ArtilleryShelling != null && instance.ArtilleryShelling.ArtilleryMapsConfigs != null &&
                instance.ArtilleryShelling.ArtilleryMapsConfigs.Keys.Contains(location.Id))
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
                GameObject gameObject = (GameObject)Resources.Load("Prefabs/HALLOWEEN_CONTROLLER");
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
                    _halloweenEventManager = gameWorld.gameObject.GetOrAddComponent<CoopHalloweenEventManager>();
                }
            }
        }

        public void InitBTRController(BackendConfigSettingsClass instance, GameWorld gameWorld, LocationSettingsClass.Location location)
        {
            if (FikaPlugin.Instance.UseBTR && instance != null && instance.BTRSettings.LocationsWithBTR.Contains(location.Id))
            {
#if DEBUG
                Logger.LogWarning("Spawning BTR controller");
#endif
                gameWorld.BtrController = new BTRControllerClass(gameWorld);
                if (IsServer)
                {
                    GlobalEventHandlerClass.Instance.SubscribeOnEvent<BtrSpawnOnThePathEvent>(OnBtrSpawn);
                }
            }
        }

        private void OnBtrSpawn(BtrSpawnOnThePathEvent spawnEvent)
        {
            GenericPacket packet = new()
            {
                NetId = 0,
                Type = EGenericSubPacketType.SpawnBTR,
                SubPacket = new BtrSpawn(spawnEvent.Position, spawnEvent.Rotation, spawnEvent.PlayerProfileId)
            };
            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public void InitializeTransitSystem(GameWorld gameWorld, BackendConfigSettingsClass instance, Profile profile,
            LocalRaidSettings localRaidSettings, LocationSettingsClass.Location location)
        {
            bool transitActive;
            if (instance == null)
            {
                transitActive = false;
            }
            else
            {
                BackendConfigSettingsClass.TransitSettingsClass transitSettings = instance.transitSettings;
                transitActive = transitSettings != null && transitSettings.active;
            }
            if (transitActive)
            {
                gameWorld.TransitController = IsServer ? new FikaHostTransitController(instance.transitSettings, location.transitParameters,
                    profile, localRaidSettings) : new FikaClientTransitController(instance.transitSettings, location.transitParameters,
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
                BackendConfigSettingsClass.GClass1554 runddansSettings = instance.runddansSettings;
                runddansActive = runddansSettings != null && runddansSettings.active;
            }
            if (runddansActive)
            {
                gameWorld.RunddansController = IsServer ? new HostRunddansController(instance.runddansSettings, location)
                    : new ClientRunddansController(instance.runddansSettings, location);
            }
            else
            {
                RunddansControllerAbstractClass.ToggleEventEnvironment(false);
            }
        }

        public virtual async Task InitializeLoot(LocationSettingsClass.Location location)
        {
            await Task.Yield();
            throw new NotImplementedException();
        }

        public Task SetupRaidCode()
        {
            string raidCode = FikaBackendUtils.RaidCode;
            if (!string.IsNullOrEmpty(raidCode))
            {
                Traverse preloaderUiTraverse = Traverse.Create(MonoBehaviourSingleton<PreloaderUI>.Instance);
                // Raid code
                preloaderUiTraverse.Field("string_3").SetValue($"{raidCode}");
                // Update version label
                preloaderUiTraverse.Method("method_6").GetValue();

                Logger.LogInfo($"MatchingType: {FikaBackendUtils.MatchingType}, Raid Code: {raidCode}");
            }

            return Task.CompletedTask;
        }

        public virtual void SetupEventsAndExfils(Player player)
        {
            throw new NotImplementedException();
        }

        public virtual void Extract(CoopPlayer player, ExfiltrationPoint exfiltrationPoint, TransitPoint transitPoint = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Used to make sure no stims or mods reset the DamageCoeff
        /// </summary>
        /// <param name="player">The <see cref="CoopPlayer"/> to run the coroutine on</param>
        /// <returns></returns>
        protected IEnumerator ExtractRoutine(CoopPlayer player, CoopGame coopGame)
        {
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
                yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// Toggles the <see cref="FikaDebug"/> menu
        /// </summary>
        /// <param name="enabled"></param>
        public void ToggleDebug(bool enabled)
        {
            if (_fikaDebug != null)
            {
                _fikaDebug.enabled = enabled;
            }
        }

        public void DestroyDebugComponent()
        {
            if (_fikaDebug != null)
            {
                ToggleDebug(false);
                GameObject.Destroy(_fikaDebug);
                _fikaDebug = null;
            }
        }

        public virtual void CleanUp()
        {
            ThrownGrenades?.Clear();

            if (_extractRoutine != null)
            {
                _abstractGame.StopCoroutine(_extractRoutine);
            }            
        }
    }
}
