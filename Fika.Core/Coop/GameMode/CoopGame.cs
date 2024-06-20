using BepInEx.Logging;
using Comfort.Common;
using CommonAssets.Scripts.Game;
using Coop.Airdrops;
using EFT;
using EFT.AssetsManager;
using EFT.Bots;
using EFT.CameraControl;
using EFT.Counters;
using EFT.EnvironmentEffect;
using EFT.Game.Spawning;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.BattleTimer;
using EFT.UI.Matchmaker;
using EFT.UI.Screens;
using EFT.Weather;
using Fika.Core.Coop.BTR;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.FreeCamera;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using Fika.Core.Networking.NatPunch;
using Fika.Core.Networking.Packets.GameWorld;
using Fika.Core.UI.Models;
using HarmonyLib;
using JsonType;
using LiteNetLib.Utils;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Utils;
using SPT.SinglePlayer.Models.Progression;
using SPT.SinglePlayer.Utils.Healing;
using SPT.SinglePlayer.Utils.Insurance;
using SPT.SinglePlayer.Utils.Progression;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.GameMode
{
    /// <summary>
    /// Coop game used in Fika
    /// </summary>
    public sealed class CoopGame : BaseLocalGame<EftGamePlayerOwner>, IBotGame, IFikaGame
    {
        public string InfiltrationPoint;
        public bool HasAddedFenceRep = false;
        public ExitStatus MyExitStatus { get; set; } = ExitStatus.Survived;
        public string MyExitLocation { get; set; } = null;
        public ISpawnSystem SpawnSystem;
        public Dictionary<string, Player> Bots = [];
        public List<int> ExtractedPlayers { get; } = [];

        private readonly Dictionary<int, int> botQueue = [];
        private Coroutine extractRoutine;
        private GClass2949 spawnPoints = null;
        private ISpawnPoint spawnPoint = null;
        private GClass579 GClass579;
        private WavesSpawnScenario wavesSpawnScenario_0;
        private NonWavesSpawnScenario nonWavesSpawnScenario_0;
        private Func<Player, EftGamePlayerOwner> func_1;
        private bool hasSaved = false;
        private CoopExfilManager exfilManager;
        private CoopTimeManager timeManager;
        private FikaDebug fikaDebug;
        private bool isServer;

        public FikaDynamicAI DynamicAI { get; private set; }
        public RaidSettings RaidSettings { get; private set; }
        public ISession BackEndSession { get => PatchConstants.BackEndSession; }
        BotsController IBotGame.BotsController
        {
            get
            {
                return botsController_0;
            }
        }
        public BotsController BotsController
        {
            get
            {
                return botsController_0;
            }
        }
        public IWeatherCurve WeatherCurve
        {
            get
            {
                return WeatherController.Instance.WeatherCurve;
            }
        }

        private static ManualLogSource Logger;

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
        internal static CoopGame Create(IInputTree inputTree, Profile profile, GameDateTime backendDateTime,
            InsuranceCompanyClass insurance, MenuUI menuUI, GameUI gameUI, LocationSettingsClass.Location location,
            TimeAndWeatherSettings timeAndWeather, WavesSettings wavesSettings, EDateTime dateTime,
            Callback<ExitStatus, TimeSpan, MetricsClass> callback, float fixedDeltaTime, EUpdateQueue updateQueue,
            ISession backEndSession, TimeSpan sessionTime, RaidSettings raidSettings)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource("CoopGame");

            bool useCustomWeather = timeAndWeather.IsRandomWeather;
            timeAndWeather.IsRandomWeather = false;

            CoopGame coopGame = smethod_0<CoopGame>(inputTree, profile, backendDateTime, insurance, menuUI, gameUI,
                location, timeAndWeather, wavesSettings, dateTime, callback, fixedDeltaTime, updateQueue, backEndSession,
                new TimeSpan?(sessionTime));

            coopGame.isServer = FikaBackendUtils.IsServer;

            // Non Waves Scenario setup
            coopGame.nonWavesSpawnScenario_0 = NonWavesSpawnScenario.smethod_0(coopGame, location, coopGame.botsController_0);
            coopGame.nonWavesSpawnScenario_0.ImplementWaveSettings(wavesSettings);

            // Waves Scenario setup
            WildSpawnWave[] waves = LocalGame.smethod_7(wavesSettings, location.waves);
            coopGame.wavesSpawnScenario_0 = WavesSpawnScenario.smethod_0(coopGame.gameObject, waves, new Action<BotWaveDataClass>(coopGame.botsController_0.ActivateBotsByWave), location);

            BossLocationSpawn[] bossSpawns = LocalGame.smethod_8(wavesSettings, location.BossLocationSpawn);
            coopGame.GClass579 = GClass579.smethod_0(bossSpawns, new Action<BossLocationSpawn>(coopGame.botsController_0.ActivateBotsByWave));

            if (useCustomWeather && coopGame.isServer)
            {
                Logger.LogInfo("Custom weather enabled, initializing curves");
                coopGame.SetupCustomWeather(timeAndWeather);
            }

            SetupGamePlayerOwnerHandler setupGamePlayerOwnerHandler = new(inputTree, insurance, backEndSession, gameUI, coopGame, location);
            coopGame.func_1 = new Func<Player, EftGamePlayerOwner>(setupGamePlayerOwnerHandler.HandleSetup);

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

            coopGame.timeManager = CoopTimeManager.Create(coopGame);

            coopGame.RaidSettings = raidSettings;

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

            public EftGamePlayerOwner HandleSetup(Player player)
            {
                EftGamePlayerOwner gamePlayerOwner = EftGamePlayerOwner.Create(player, inputTree, insurance, backEndSession, gameUI, game.GameDateTime, location);
                gamePlayerOwner.OnLeave += game.vmethod_3;
                return gamePlayerOwner;
            }
        }

        /// <summary>
        /// Sets up a custom weather curve
        /// </summary>
        /// <param name="timeAndWeather">Struct with custom settings</param>
        private void SetupCustomWeather(TimeAndWeatherSettings timeAndWeather)
        {
            if (WeatherController.Instance == null)
            {
                return;
            }

            DateTime dateTime = GClass1304.StartOfDay();
            DateTime dateTime2 = dateTime.AddDays(1);

            WeatherClass weather = WeatherClass.CreateDefault();
            WeatherClass weather2 = WeatherClass.CreateDefault();
            weather.Cloudness = weather2.Cloudness = timeAndWeather.CloudinessType.ToValue();
            weather.Rain = weather2.Rain = timeAndWeather.RainType.ToValue();
            weather.Wind = weather2.Wind = timeAndWeather.WindType.ToValue();
            weather.ScaterringFogDensity = weather2.ScaterringFogDensity = timeAndWeather.FogType.ToValue();
            weather.Time = dateTime.Ticks;
            weather2.Time = dateTime2.Ticks;
            WeatherController.Instance.method_0([weather, weather2]);
        }

        public override void SetMatchmakerStatus(string status, float? progress = null)
        {
            if (GClass3131.Instance.CurrentScreenController is MatchmakerTimeHasCome.GClass3187 gclass)
            {
                gclass.ChangeStatus(status, progress);
            }
        }

        /// <summary>
        /// Creates and initializes the <see cref="CoopHandler"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="MissingReferenceException">If no ServerId was found</exception>
        public Task CreateCoopHandler()
        {
            Logger.LogInfo("Creating CoopHandler...");
            CoopHandler coopHandler = CoopHandler.GetCoopHandler();
            if (coopHandler != null)
            {
                Destroy(coopHandler);
            }

            if (CoopHandler.CoopHandlerParent != null)
            {
                Destroy(CoopHandler.CoopHandlerParent);
                CoopHandler.CoopHandlerParent = null;
            }

            if (CoopHandler.CoopHandlerParent == null)
            {
                CoopHandler.CoopHandlerParent = new GameObject("CoopHandlerParent");
                DontDestroyOnLoad(CoopHandler.CoopHandlerParent);
            }

            coopHandler = CoopHandler.CoopHandlerParent.AddComponent<CoopHandler>();
            coopHandler.LocalGameInstance = this;

            if (!string.IsNullOrEmpty(FikaBackendUtils.GetGroupId()))
            {
                coopHandler.ServerId = FikaBackendUtils.GetGroupId();
            }
            else
            {
                Destroy(coopHandler);
                Logger.LogError("No Server Id found, Deleting Coop Handler");
                throw new MissingReferenceException("No Server Id found");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns all human players
        /// </summary>
        /// <param name="coopHandler"><see cref="CoopHandler"/> used to fetch players</param>
        /// <returns></returns>
        private List<CoopPlayer> GetPlayers(CoopHandler coopHandler)
        {
            List<CoopPlayer> humanPlayers = [];

            // Grab all players
            foreach (CoopPlayer player in coopHandler.Players.Values)
            {
                if ((player.IsYourPlayer || player is ObservedCoopPlayer) && player.HealthController.IsAlive)
                {
                    humanPlayers.Add(player);
                }
            }
            return humanPlayers;
        }

        /// <summary>
        /// Calculates the distance from all players
        /// </summary>
        /// <param name="position">The <see cref="Vector3"/> position</param>
        /// <param name="humanPlayers"><see cref="List{T}"/> of all human <see cref="CoopPlayer"/>s</param>
        /// <returns></returns>
        private float GetDistanceFromPlayers(Vector3 position, List<CoopPlayer> humanPlayers)
        {
            float distance = float.PositiveInfinity;

            foreach (Player player in humanPlayers)
            {
                float tempDistance = Vector3.SqrMagnitude(position - player.Position);

                if (tempDistance < distance) // Get the closest distance to any player. so we dont despawn bots in a players face.
                {
                    distance = tempDistance;
                }
            }
            return distance;
        }

        /// <summary>
        /// Grabs the bot furthest away from all players and returns its distance
        /// </summary>
        /// <param name="humanPlayers">List of all human <see cref="CoopPlayer"/>s</param>
        /// <param name="furthestDistance">The furthest <see cref="float"/> distance</param>
        /// <returns></returns>
        private string GetFurthestBot(List<CoopPlayer> humanPlayers, out float furthestDistance)
        {
            string furthestBot = string.Empty;
            furthestDistance = 0f;

            foreach (var botKeyValuePair in Bots)
            {
                if (IsInvalidBotForDespawning(botKeyValuePair))
                {
                    continue;
                }

                float tempDistance = GetDistanceFromPlayers(botKeyValuePair.Value.Position, humanPlayers);

                if (tempDistance > furthestDistance) // We still want the furthest bot.
                {
                    furthestDistance = tempDistance;
                    furthestBot = botKeyValuePair.Key;
                }
            }

            return furthestBot;
        }

        /// <summary>
        /// Checks whether this bot is valid for despawning
        /// </summary>
        /// <param name="kvp"><see cref="KeyValuePair{TKey, TValue}"/> of <see cref="string"/> profileId and <see cref="Player"/> player</param>
        /// <returns></returns>
        private bool IsInvalidBotForDespawning(KeyValuePair<string, Player> kvp)
        {
            if (kvp.Value == null || kvp.Value == null || kvp.Value.Position == null)
            {
#if DEBUG
                Logger.LogWarning("Bot is null, skipping");
#endif
                return true;
            }

            CoopBot coopBot = (CoopBot)kvp.Value;

            if (coopBot != null)
            {
#if DEBUG
                Logger.LogWarning("Bot is not started, skipping");
#endif
                return true;
            }

            WildSpawnType role = kvp.Value.Profile.Info.Settings.Role;

            if (role is not WildSpawnType.pmcUSEC and not WildSpawnType.pmcBEAR and not WildSpawnType.assault)
            {
                // We skip all the bots that are not pmcUSEC, pmcBEAR or assault. That means we never remove bosses, bossfollowers, and raiders
                return true;
            }

            return false;
        }

        /// <summary>
        /// Used to spawn a bot for the host
        /// </summary>
        /// <param name="profile"><see cref="Profile"/> to spawn</param>
        /// <param name="position">The <see cref="Vector3"/> position to spawn on</param>
        /// <returns></returns>
        private async Task<LocalPlayer> CreateBot(Profile profile, Vector3 position)
        {
#if DEBUG
            Logger.LogWarning($"Creating bot {profile.Info.Settings.Role} at {position}");
#endif
            if (!isServer)
            {
                return null;
            }

            if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                Logger.LogError($"{nameof(CreateBot)}: Unable to find {nameof(CoopHandler)}");
                return null;
            }

            WildSpawnType role = profile.Info.Settings.Role;
            bool isSpecial = false;
            if (role is not WildSpawnType.pmcUSEC and not WildSpawnType.pmcBEAR and not WildSpawnType.assault)
            {
#if DEBUG
                Logger.LogWarning($"Bot {profile.Info.Settings.Role} is a special bot.");
#endif
                isSpecial = true;
            }

            if (FikaPlugin.EnforcedSpawnLimits.Value && Bots.Count >= botsController_0.BotSpawner.MaxBots)
            {
                bool despawned = false;

                if (FikaPlugin.DespawnFurthest.Value)
                {
                    despawned = TryDespawnFurthestBot(profile, position, coopHandler);
                }

                // If it's not special and we didnt despawn something, we dont spawn a new bot.
                if (!isSpecial && !despawned)
                {
#if DEBUG
                    Logger.LogWarning($"Stopping spawn of bot {profile.Nickname}, max count reached and enforced limits enabled. Current: {Bots.Count}, Max: {botsController_0.BotSpawner.MaxBots}, Alive & Loading: {botsController_0.BotSpawner.AliveAndLoadingBotsCount}");
#endif
                    return null;
                }
            }

            int netId = 1000;
            LocalPlayer localPlayer;

            if (!Status.IsRunned())
            {
                localPlayer = null;
            }
            else if (Bots.ContainsKey(profile.Id))
            {
                localPlayer = null;
            }
            else
            {
                int num = method_12();
                profile.SetSpawnedInSession(profile.Info.Side == EPlayerSide.Savage);

                FikaServer server = Singleton<FikaServer>.Instance;
                netId = server.PopNetId();

                SendCharacterPacket packet = new(new FikaSerialization.PlayerInfoPacket(profile), true, true, position, netId);
                Singleton<FikaServer>.Instance.SendDataToAll(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableUnordered);

                if (server.NetServer.ConnectedPeersCount > 0)
                {
                    await WaitForPlayersToLoadBotProfile(netId);
                }

                localPlayer = await CoopBot.CreateBot(num, position, Quaternion.identity, "Player",
                   "Bot_", EPointOfView.ThirdPerson, profile, true, UpdateQueue, Player.EUpdateMode.Manual,
                   Player.EUpdateMode.Auto, BackendConfigAbstractClass.Config.CharacterController.BotPlayerMode, () => 1f,
                   () => 1f, GClass1457.Default);

                localPlayer.Location = Location_0.Id;

                if (Bots.ContainsKey(localPlayer.ProfileId))
                {
                    Destroy(localPlayer);
                    return null;
                }
                else
                {
#if DEBUG
                    Logger.LogInfo($"Bot {profile.Info.Settings.Role} created at {position} SUCCESSFULLY!");
#endif
                    Bots.Add(localPlayer.ProfileId, localPlayer);
                }

                if (profile.Info.Side is EPlayerSide.Bear or EPlayerSide.Usec)
                {
                    Slot backpackSlot = profile.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack);
                    Item backpack = backpackSlot.ContainedItem;
                    if (backpack != null)
                    {
                        Item[] items = backpack.GetAllItems()?.ToArray();
                        if (items != null)
                        {
                            for (int i = 0; i < items.Count(); i++)
                            {
                                Item item = items[i];
                                if (item == backpack)
                                {
                                    continue;
                                }

                                item.SpawnedInSession = true;
                            }
                        }
                    }
                }

                if (Singleton<GameWorld>.Instance != null)
                {
                    if (!Singleton<GameWorld>.Instance.RegisteredPlayers.Any(x => x.ProfileId == localPlayer.ProfileId))
                    {
                        Singleton<GameWorld>.Instance.RegisterPlayer(localPlayer);
                    }
                }
                else
                {
                    Logger.LogError("Cannot add player because GameWorld is NULL");
                }
            }

            CoopBot coopBot = (CoopBot)localPlayer;
            coopBot.NetId = netId;
            if (FikaPlugin.DisableBotMetabolism.Value)
            {
                coopBot.HealthController.DisableMetabolism();
            }
            coopHandler.Players.Add(coopBot.NetId, coopBot);

            return localPlayer;
        }

        /// <summary>
        /// Increments the amount of players that have loaded a bot, used for <see cref="WaitForPlayersToLoadBotProfile(int)"/>
        /// </summary>
        /// <param name="netId"></param>
        public void IncreaseLoadedPlayers(int netId)
        {
            if (botQueue.ContainsKey(netId))
            {
                botQueue[netId]++;
            }
            else
            {
                Logger.LogError($"IncreaseLoadedPlayers: could not find netId {netId}!");
            }
        }

        /// <summary>
        /// <see cref="Task"/> used to ensure that all players loads a bot before it spawns
        /// </summary>
        /// <param name="netId">The NetId to spawn</param>
        /// <returns></returns>
        private async Task WaitForPlayersToLoadBotProfile(int netId)
        {
            botQueue.Add(netId, 0);
            DateTime start = DateTime.Now;
            int connectedPeers = Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount;

            while (botQueue[netId] < connectedPeers)
            {
                if (start.Subtract(DateTime.Now).TotalSeconds >= 30) // ~30 second failsafe
                {
                    Logger.LogWarning("WaitForPlayersToLoadBotProfile: Took too long to receive all packets!");
                    botQueue.Remove(netId);
                    return;
                }

                await Task.Delay(250);
            }

            botQueue.Remove(netId);
        }

        /// <summary>
        /// Tries to despawn the furthest bot from all players
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="position"></param>
        /// <param name="coopHandler"></param>
        /// <returns></returns>
        private bool TryDespawnFurthestBot(Profile profile, Vector3 position, CoopHandler coopHandler)
        {
            List<CoopPlayer> humanPlayers = GetPlayers(coopHandler);

            string botKey = GetFurthestBot(humanPlayers, out float furthestDistance);

            if (botKey == string.Empty)
            {
#if DEBUG
                Logger.LogWarning("TryDespawnFurthest: botKey was empty");
#endif
                return false;
            }

            if (furthestDistance > GetDistanceFromPlayers(position, humanPlayers))
            {
#if DEBUG
                Logger.LogWarning($"We're not despawning anything. The furthest bot is closer than the one we wanted to spawn.");
#endif
                return false;
            }

            //Dont despawn inside of dynamic AI range
            if (furthestDistance < FikaPlugin.DespawnMinimumDistance.Value * FikaPlugin.DespawnMinimumDistance.Value) //Square it because we use sqrMagnitude for distance calculation
            {
#if DEBUG
                Logger.LogWarning($"We're not despawning anything. Furthest despawnable bot is inside minimum despawn range.");
#endif
                return false;
            }
            Player bot = Bots[botKey];
#if DEBUG
            Logger.LogWarning($"Removing {bot.Profile.Info.Settings.Role} at a distance of {Math.Sqrt(furthestDistance)}m from its nearest player.");
#endif
            DespawnBot(coopHandler, bot);
#if DEBUG
            Logger.LogWarning($"Bot {bot.Profile.Info.Settings.Role} despawned successfully.");
#endif
            return true;
        }

        /// <summary>
        /// Despawns a bot
        /// </summary>
        /// <param name="coopHandler"></param>
        /// <param name="bot">The bot to despawn</param>
        private void DespawnBot(CoopHandler coopHandler, Player bot)
        {
            IBotGame botGame = Singleton<IBotGame>.Instance;
            BotOwner botOwner = bot.AIData.BotOwner;

            BotsController.Bots.Remove(botOwner);
            bot.HealthController.DiedEvent -= botOwner.method_6; // Unsubscribe from the event to prevent errors.
            BotUnspawn(botOwner);
            if (botOwner != null)
            {
                botOwner.Dispose();
            }

            CoopPlayer coopPlayer = (CoopPlayer)bot;
            coopHandler.Players.Remove(coopPlayer.NetId);
            Bots.Remove(bot.ProfileId);
        }

        /// <summary>
        /// The countdown deploy screen
        /// </summary>
        /// <returns></returns>
        public override IEnumerator vmethod_1()
        {
            CoopPlayer coopPlayer = (CoopPlayer)PlayerOwner.Player;
            coopPlayer.PacketSender.Init();

            int timeBeforeDeployLocal = Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal;
            DateTime dateTime = GClass1304.Now.AddSeconds(timeBeforeDeployLocal);
            new MatchmakerFinalCountdown.GClass3186(Profile_0, dateTime).ShowScreen(EScreenState.Root);
            MonoBehaviourSingleton<BetterAudio>.Instance.FadeInVolumeBeforeRaid(timeBeforeDeployLocal);
            Singleton<GUISounds>.Instance.StopMenuBackgroundMusicWithDelay(timeBeforeDeployLocal);
            GameUi.gameObject.SetActive(true);
            GameUi.TimerPanel.ProfileId = ProfileId;
            yield return new WaitForSeconds(timeBeforeDeployLocal);
        }

        /// <summary>
        /// This task ensures that all players are joined and loaded before continuing
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForOtherPlayers()
        {
            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                if (isServer && FikaBackendUtils.HostExpectedNumberOfPlayers <= 1)
                {
                    if (DynamicAI != null)
                    {
                        DynamicAI.AddHumans();
                    }

                    SetStatusModel status = new(coopHandler.MyPlayer.ProfileId, LobbyEntry.ELobbyStatus.IN_GAME);
                    Task updateStatus = FikaRequestHandler.UpdateSetStatus(status);

                    while (!updateStatus.IsCompleted)
                    {
                        yield return null;
                    }

                    Singleton<FikaServer>.Instance.ReadyClients++;
                    yield break;
                }

                NetDataWriter writer = new();

                FikaBackendUtils.ScreenController.ChangeStatus("Waiting for other players to finish loading...");

                int expectedPlayers = FikaBackendUtils.HostExpectedNumberOfPlayers;

                if (isServer)
                {
                    SetStatusModel status = new(coopHandler.MyPlayer.ProfileId, LobbyEntry.ELobbyStatus.IN_GAME);
                    Task updateStatus = FikaRequestHandler.UpdateSetStatus(status);

                    while (!updateStatus.IsCompleted)
                    {
                        yield return null;
                    }

                    do
                    {
                        yield return null;
                    } while (coopHandler.HumanPlayers < expectedPlayers);

                    FikaServer server = Singleton<FikaServer>.Instance;
                    server.ReadyClients++;
                    InformationPacket packet = new()
                    {
                        NumberOfPlayers = server.NetServer.ConnectedPeersCount,
                        ReadyPlayers = server.ReadyClients
                    };
                    writer.Reset();
                    server.SendDataToAll(writer, ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);

                    do
                    {
                        FikaBackendUtils.ScreenController.ChangeStatus("Waiting for other players to finish loading...", server.ReadyClients / expectedPlayers);
                        yield return new WaitForEndOfFrame();
                    } while (server.ReadyClients < expectedPlayers);

                    foreach (CoopPlayer player in coopHandler.Players.Values)
                    {
                        SyncNetIdPacket syncPacket = new(player.ProfileId, player.NetId);

                        writer.Reset();
                        Singleton<FikaServer>.Instance.SendDataToAll(writer, ref syncPacket, LiteNetLib.DeliveryMethod.ReliableUnordered);
                    }

                    if (DynamicAI != null)
                    {
                        DynamicAI.AddHumans();
                    }
                }
                else
                {
                    do
                    {
                        yield return null;
                    } while (coopHandler.HumanPlayers < expectedPlayers);

                    FikaClient client = Singleton<FikaClient>.Instance;
                    InformationPacket packet = new(true)
                    {
                        ReadyPlayers = 1
                    };
                    writer.Reset();
                    client.SendData(writer, ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);

                    do
                    {
                        FikaBackendUtils.ScreenController.ChangeStatus("Waiting for other players to finish loading...", client.ReadyClients / expectedPlayers);
                        yield return new WaitForEndOfFrame();
                    } while (client.ReadyClients < expectedPlayers);
                }
            }
        }

        /// <summary>
        /// Sends or receives the <see cref="ISpawnPoint"/> for the game
        /// </summary>
        /// <returns></returns>
        private async Task SendOrReceiveSpawnPoint()
        {
            if (isServer)
            {
                bool spawnTogether = RaidSettings.PlayersSpawnPlace == EPlayersSpawnPlace.SamePlace;
                UpdateSpawnPointRequest body = new(spawnTogether ? spawnPoint.Id : "");
                if (spawnTogether)
                {
                    Logger.LogInfo($"Setting Spawn Point to: {spawnPoint.Id}");
                }
                else
                {
                    Logger.LogInfo("Using random spawn points!");
                    NotificationManagerClass.DisplayMessageNotification("Using random spawn points", iconType: EFT.Communications.ENotificationIconType.Alert);
                }
                await FikaRequestHandler.UpdateSpawnPoint(body);
            }
            else
            {
                SpawnPointRequest body = new();
                SpawnPointResponse response = await FikaRequestHandler.RaidSpawnPoint(body);
                string name = response.SpawnPoint;

                if (!string.IsNullOrEmpty(name))
                {
                    Logger.LogInfo($"Retrieved Spawn Point '{name}' from server");

                    Dictionary<ISpawnPoint, SpawnPointMarker> allSpawnPoints = Traverse.Create(spawnPoints).Field<Dictionary<ISpawnPoint, SpawnPointMarker>>("dictionary_0").Value;
                    foreach (ISpawnPoint spawnPointObject in allSpawnPoints.Keys)
                    {
                        if (spawnPointObject.Id == name)
                        {
                            spawnPoint = spawnPointObject;
                        }
                    }
                }
                else
                {
                    Logger.LogInfo("Spawn Point was empty, selecting random.");
                    NotificationManagerClass.DisplayMessageNotification("Using random spawn points", iconType: EFT.Communications.ENotificationIconType.Alert);
                    spawnPoint = SpawnSystem.SelectSpawnPoint(ESpawnCategory.Player, Profile_0.Info.Side);
                }
            }
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
        public override async Task<LocalPlayer> vmethod_2(int playerId, Vector3 position, Quaternion rotation,
            string layerName, string prefix, EPointOfView pointOfView, Profile profile, bool aiControl,
            EUpdateQueue updateQueue, Player.EUpdateMode armsUpdateMode, Player.EUpdateMode bodyUpdateMode,
            CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity, Func<float> getAimingSensitivity,
            IStatisticsManager statisticsManager, AbstractQuestControllerClass questController, AbstractAchievementControllerClass achievementsController)
        {
            await CreateCoopHandler();

            profile.SetSpawnedInSession(profile.Side == EPlayerSide.Savage);

            LocalPlayer myPlayer = await CoopPlayer.Create(playerId, spawnPoint.Position, spawnPoint.Rotation, "Player",
                "Main_", EPointOfView.FirstPerson, profile, false, UpdateQueue, armsUpdateMode, bodyUpdateMode,
                BackendConfigAbstractClass.Config.CharacterController.ClientPlayerMode, getSensitivity,
                getAimingSensitivity, new GClass1456(), isServer ? 0 : 1000, statisticsManager);

            await NetManagerUtils.InitNetManager(isServer);

            if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                Logger.LogError($"{nameof(vmethod_2)}:Unable to find {nameof(CoopHandler)}");
                throw new MissingComponentException("CoopHandler was missing during CoopGame init");
            }

            if (RaidSettings.MetabolismDisabled)
            {
                myPlayer.HealthController.DisableMetabolism();
                NotificationManagerClass.DisplayMessageNotification("Metabolism disabled", iconType: EFT.Communications.ENotificationIconType.Alert);
            }

            CoopPlayer coopPlayer = (CoopPlayer)myPlayer;
            coopHandler.Players.Add(coopPlayer.NetId, coopPlayer);

            PlayerSpawnRequest body = new(myPlayer.ProfileId, FikaBackendUtils.GetGroupId());
            await FikaRequestHandler.UpdatePlayerSpawn(body);

            myPlayer.SpawnPoint = spawnPoint;

            GameObject customButton = null;

            await NetManagerUtils.SetupGameVariables(isServer, coopPlayer);

            // This creates a "custom" Back button so that we can back out if we get stuck
            if (MenuUI.Instantiated)
            {
                MenuUI menuUI = MenuUI.Instance;
                DefaultUIButton backButton = Traverse.Create(menuUI.MatchmakerTimeHasCome).Field<DefaultUIButton>("_cancelButton").Value;
                customButton = Instantiate(backButton.gameObject, backButton.gameObject.transform.parent);
                customButton.gameObject.name = "FikaBackButton";
                customButton.gameObject.transform.position = new(customButton.transform.position.x, customButton.transform.position.y - 20, customButton.transform.position.z);
                customButton.gameObject.SetActive(true);
                DefaultUIButton backButtonComponent = customButton.GetComponent<DefaultUIButton>();
                backButtonComponent.SetHeaderText("Cancel", 32);
                backButtonComponent.SetEnabledTooltip("EXPERIMENTAL: Cancels the matchmaking and returns to the menu.");
                UnityEngine.Events.UnityEvent newEvent = new();
                newEvent.AddListener(() =>
                {
                    Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("WARNING", 
                        message: "Backing out from this stage is currently experimental. It is recommended to ALT+F4 instead. Do you still want to continue?",
                        ErrorScreen.EButtonType.OkButton, 15f, () =>
                        {
                            StopFromCancel(myPlayer.ProfileId, ExitStatus.Runner);
                            PlayerLeftRequest playerLeftRequest = new(coopPlayer.ProfileId);
                            FikaRequestHandler.RaidLeave(playerLeftRequest);
                        }, null);
                });
                Traverse.Create(backButtonComponent).Field("OnClick").SetValue(newEvent);
            }

            SendCharacterPacket packet = new(new FikaSerialization.PlayerInfoPacket(myPlayer.Profile), myPlayer.HealthController.IsAlive, false, myPlayer.Transform.position, (myPlayer as CoopPlayer).NetId);

            if (isServer)
            {
                await SetStatus(myPlayer, LobbyEntry.ELobbyStatus.COMPLETE);
            }
            else
            {
                Singleton<FikaClient>.Instance.SendData(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableUnordered);
            }

            await WaitForPlayers();

            if(isServer && FikaPlugin.UseNatPunching.Value)
            {
                FikaNatPunchServer natPunchServer = Singleton<FikaServer>.Instance.FikaNatPunchServer;
                
                if (natPunchServer != null && natPunchServer.Connected)
                {
                    natPunchServer.Close();
                }
            }

            fikaDebug = gameObject.AddComponent<FikaDebug>();

            Destroy(customButton);

            return myPlayer;
        }

        /// <summary>
        /// Initializes the local player
        /// </summary>
        /// <param name="botsSettings"></param>
        /// <param name="backendUrl"></param>
        /// <param name="runCallback"></param>
        /// <returns></returns>
        public async Task InitPlayer(BotControllerSettings botsSettings, string backendUrl, Callback runCallback)
        {
            Status = GameStatus.Running;
            UnityEngine.Random.InitState((int)GClass1304.Now.Ticks);

            LocationSettingsClass.Location location;
            if (Location_0.IsHideout)
            {
                location = Location_0;
            }
            else
            {
                using (GClass21.StartWithToken("LoadLocation"))
                {
                    int num = UnityEngine.Random.Range(1, 6);
                    method_6(backendUrl, Location_0.Id, num);
                    location = await ginterface158_0.LoadLocationLoot(Location_0.Id, num);
                }
            }

            BackendConfigSettingsClass instance = Singleton<BackendConfigSettingsClass>.Instance;
            if (instance != null && instance.EventSettings.EventActive && !instance.EventSettings.LocationsToIgnore.Contains(location._Id))
            {
                GameObject gameObject = (GameObject)Resources.Load("Prefabs/HALLOWEEN_CONTROLLER");
                if (gameObject != null)
                {
                    transform.InstantiatePrefab(gameObject);
                }
                else
                {
                    Logger.LogError("Can't find event prefab in resources. Path: 'Prefabs/HALLOWEEN_CONTROLLER'");
                }
            }
            ApplicationConfigClass config = BackendConfigAbstractClass.Config;
            if (config.FixedFrameRate > 0f)
            {
                FixedDeltaTime = 1f / config.FixedFrameRate;
            }

            using (GClass21.StartWithToken("player create"))
            {
                Player player = await CreateLocalPlayer();
                dictionary_0.Add(player.ProfileId, player);
                gparam_0 = func_1(player);
                PlayerCameraController.Create(gparam_0.Player);
                CameraClass.Instance.SetOcclusionCullingEnabled(Location_0.OcculsionCullingEnabled);
                CameraClass.Instance.IsActive = false;
            }

            StartHandler startHandler = new(this, botsSettings, SpawnSystem, runCallback);

            await method_11(location, startHandler.FinishLoading);
        }

        /// <summary>
        /// Handler used to start the game
        /// </summary>
        /// <param name="localGame"></param>
        /// <param name="botSettings"></param>
        /// <param name="spawnSystem"></param>
        /// <param name="runCallback"></param>
        private class StartHandler(BaseLocalGame<EftGamePlayerOwner> localGame, BotControllerSettings botSettings, ISpawnSystem spawnSystem, Callback runCallback)
        {
            private readonly BaseLocalGame<EftGamePlayerOwner> localGame = localGame;
            private readonly BotControllerSettings botSettings = botSettings;
            private readonly ISpawnSystem spawnSystem = spawnSystem;
            private readonly Callback runCallback = runCallback;

            public void FinishLoading()
            {
                localGame.method_5(botSettings, spawnSystem, runCallback);
            }
        }

        /// <summary>
        /// Creates the local player
        /// </summary>
        /// <returns>A <see cref="Player"/></returns>
        private async Task<Player> CreateLocalPlayer()
        {
            int num = method_12();

            Player.EUpdateMode eupdateMode = Player.EUpdateMode.Auto;
            if (BackendConfigAbstractClass.Config.UseHandsFastAnimator)
            {
                eupdateMode = Player.EUpdateMode.Manual;
            }

            spawnPoints = GClass2949.CreateFromScene(new DateTime?(GClass1304.LocalDateTimeFromUnixTime(Location_0.UnixDateTime)), Location_0.SpawnPointParams);
            int spawnSafeDistance = (Location_0.SpawnSafeDistanceMeters > 0) ? Location_0.SpawnSafeDistanceMeters : 100;
            GStruct379 settings = new(Location_0.MinDistToFreePoint, Location_0.MaxDistToFreePoint, Location_0.MaxBotPerZone, spawnSafeDistance);
            SpawnSystem = GClass2950.CreateSpawnSystem(settings, new Func<float>(Class1384.class1384_0.method_0), Singleton<GameWorld>.Instance, zones: botsController_0, spawnPoints);

            if (isServer)
            {
                spawnPoint = SpawnSystem.SelectSpawnPoint(ESpawnCategory.Player, Profile_0.Info.Side);
                await SendOrReceiveSpawnPoint();
            }

            if (!isServer)
            {
                await SendOrReceiveSpawnPoint();
                if (spawnPoint == null)
                {
                    Logger.LogWarning("SpawnPoint was null after retrieving it from the server!");
                    spawnPoint = SpawnSystem.SelectSpawnPoint(ESpawnCategory.Player, Profile_0.Info.Side);
                }
            }

            IStatisticsManager statisticsManager = new CoopClientStatisticsManager(Profile_0);

            LocalPlayer myPlayer = await vmethod_2(num, spawnPoint.Position, spawnPoint.Rotation, "Player", "Main_", EPointOfView.FirstPerson, Profile_0, false,
                UpdateQueue, eupdateMode, Player.EUpdateMode.Auto, BackendConfigAbstractClass.Config.CharacterController.ClientPlayerMode,
                new Func<float>(Class1384.class1384_0.method_3), new Func<float>(Class1384.class1384_0.method_4),
                statisticsManager, null, null);

            myPlayer.Location = Location_0.Id;
            myPlayer.OnEpInteraction += OnEpInteraction;

            return myPlayer;
        }

        /// <summary>
        /// <see cref="Task"/> used to wait for all other players to join the game
        /// </summary>
        /// <returns></returns>
        private async Task WaitForPlayers()
        {
            Logger.LogInfo("Starting task to wait for other players.");

            if (FikaBackendUtils.ScreenController != null)
            {
                FikaBackendUtils.ScreenController.ChangeStatus($"Initializing Coop Game...");
            }
            int numbersOfPlayersToWaitFor = 0;

            if (isServer)
            {
                FikaServer server = Singleton<FikaServer>.Instance;

                do
                {
                    numbersOfPlayersToWaitFor = FikaBackendUtils.HostExpectedNumberOfPlayers - (server.NetServer.ConnectedPeersCount + 1);
                    if (FikaBackendUtils.ScreenController != null)
                    {
                        if (numbersOfPlayersToWaitFor > 0)
                        {
                            FikaBackendUtils.ScreenController.ChangeStatus($"Waiting for {numbersOfPlayersToWaitFor} {(numbersOfPlayersToWaitFor > 1 ? "players" : "player")}");
                        }
                        else
                        {
                            FikaBackendUtils.ScreenController.ChangeStatus($"All players joined, starting game...");
                        }
                    }
                    else
                    {
                        Logger.LogError("WaitForPlayers::GClass3163 was null!");
                    }
                    await Task.Delay(100);
                } while (numbersOfPlayersToWaitFor > 0);
            }
            else
            {
                FikaClient client = Singleton<FikaClient>.Instance;

                while (client.NetClient == null)
                {
                    await Task.Delay(500);
                }

                int connectionAttempts = 0;

                while (client.ServerConnection == null && connectionAttempts < 5)
                {
                    // Server retries 10 times with a 500ms interval, we give it 5 seconds to try
                    FikaBackendUtils.ScreenController.ChangeStatus("Waiting for client to connect to server... If there is no notification it failed.");
                    connectionAttempts++;
                    await Task.Delay(1000);

                    if (client.ServerConnection == null && connectionAttempts == 5)
                    {
                        Singleton<PreloaderUI>.Instance.ShowErrorScreen("Network Error",
                            "Unable to connect to the raid server. Make sure ports are forwarded and/or UPnP is enabled and supported.");
                    }
                }

                while (client == null)
                {
                    await Task.Delay(500);
                }

                InformationPacket packet = new(true);
                NetDataWriter writer = new();
                writer.Reset();
                client.SendData(writer, ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                do
                {
                    numbersOfPlayersToWaitFor = FikaBackendUtils.HostExpectedNumberOfPlayers - (client.ConnectedClients + 1);
                    if (FikaBackendUtils.ScreenController != null)
                    {
                        if (numbersOfPlayersToWaitFor > 0)
                        {
                            FikaBackendUtils.ScreenController.ChangeStatus($"Waiting for {numbersOfPlayersToWaitFor} {(numbersOfPlayersToWaitFor > 1 ? "players" : "player")}");
                        }
                        else
                        {
                            FikaBackendUtils.ScreenController.ChangeStatus($"All players joined, starting game...");
                        }
                    }
                    else
                    {
                        Logger.LogError("WaitForPlayers::GClass3163 was null!");
                    }
                    packet = new(true);
                    writer.Reset();
                    client.SendData(writer, ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    await Task.Delay(1000);
                } while (numbersOfPlayersToWaitFor > 0);
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
        /// <param name="runCallback"></param>
        /// <returns></returns>
        public override IEnumerator vmethod_4(BotControllerSettings controllerSettings, ISpawnSystem spawnSystem, Callback runCallback)
        {
#if DEBUG
            Logger.LogWarning("vmethod_4");
#endif

            if (!isServer)
            {
                controllerSettings.BotAmount = EBotAmount.NoBots;
            }

            if (isServer)
            {
                BotsPresets profileCreator = new(BackEndSession, wavesSpawnScenario_0.SpawnWaves,
                    GClass579.BossSpawnWaves, nonWavesSpawnScenario_0.GClass1478_0, false);

                GClass814 botCreator = new(this, profileCreator, CreateBot);
                BotZone[] botZones = LocationScene.GetAllObjects<BotZone>(false).ToArray();

                bool enableWaves = controllerSettings.BotAmount == EBotAmount.Horde;

                botsController_0.Init(this, botCreator, botZones, spawnSystem, wavesSpawnScenario_0.BotLocationModifier,
                    controllerSettings.IsEnabled, controllerSettings.IsScavWars, enableWaves, false,
                    GClass579.HaveSectants, Singleton<GameWorld>.Instance, Location_0.OpenZones);

                Logger.LogInfo($"Location: {Location_0.Name}");

                int numberOfBots = controllerSettings.BotAmount switch
                {
                    EBotAmount.AsOnline => 20,
                    EBotAmount.NoBots => 0,
                    EBotAmount.Low => 15,
                    EBotAmount.Medium => 20,
                    EBotAmount.High => 25,
                    EBotAmount.Horde => 35,
                    _ => 0,
                };

                if (!isServer)
                {
                    numberOfBots = 0;
                }

                botsController_0.SetSettings(numberOfBots, BackEndSession.BackEndConfig.BotPresets, BackEndSession.BackEndConfig.BotWeaponScatterings);
                botsController_0.AddActivePLayer(PlayerOwner.Player);

                if (FikaPlugin.EnforcedSpawnLimits.Value)
                {
                    int limits = Location_0.Id.ToLower() switch
                    {
                        "factory4_day" => FikaPlugin.MaxBotsFactory.Value,
                        "factory4_night" => FikaPlugin.MaxBotsFactory.Value,
                        "bigmap" => FikaPlugin.MaxBotsCustoms.Value,
                        "interchange" => FikaPlugin.MaxBotsInterchange.Value,
                        "rezervbase" => FikaPlugin.MaxBotsReserve.Value,
                        "woods" => FikaPlugin.MaxBotsWoods.Value,
                        "shoreline" => FikaPlugin.MaxBotsShoreline.Value,
                        "tarkovstreets" => FikaPlugin.MaxBotsStreets.Value,
                        "sandbox" => FikaPlugin.MaxBotsGroundZero.Value,
                        "laboratory" => FikaPlugin.MaxBotsLabs.Value,
                        "lighthouse" => FikaPlugin.MaxBotsLighthouse.Value,
                        _ => 0
                    };

                    if (limits > 0)
                    {
                        botsController_0.BotSpawner.SetMaxBots(limits);
                    }
                }

                DynamicAI = gameObject.AddComponent<FikaDynamicAI>();
            }
            else
            {
                BotsPresets profileCreator = new(BackEndSession, [], [], [], false);

                GClass814 botCreator = new(this, profileCreator, CreateBot);
                BotZone[] botZones = LocationScene.GetAllObjects<BotZone>(false).ToArray();

                // Setting this to an empty array stops the client from downloading bots
                typeof(BotsController).GetField("_allTypes", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(botsController_0, new WildSpawnType[0]);

                botsController_0.Init(this, botCreator, botZones, spawnSystem, wavesSpawnScenario_0.BotLocationModifier,
                    false, false, true, false, false, Singleton<GameWorld>.Instance, Location_0.OpenZones);

                botsController_0.SetSettings(0, [], []);

                Logger.LogInfo($"Location: {Location_0.Name}");
            }

            BackendConfigSettingsClass instance = Singleton<BackendConfigSettingsClass>.Instance;

            LocalGame.Class1391 seasonTaskHandler = new();
            ESeason season = ginterface158_0.Season;
            Class394 seasonHandler = new();
            Singleton<GameWorld>.Instance.GInterface26_0 = seasonHandler;
            seasonTaskHandler.task = seasonHandler.Run(season);
            yield return new WaitUntil(new Func<bool>(seasonTaskHandler.method_0));

            int timeBeforeDeployLocal = Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal;

            if (timeBeforeDeployLocal < 5)
            {
                timeBeforeDeployLocal = 5;
                NotificationManagerClass.DisplayWarningNotification("You have set the deploy timer too low, resetting to 5!");
            }

            yield return WaitForOtherPlayers();


            int expectedPlayers = FikaBackendUtils.HostExpectedNumberOfPlayers;
            if (isServer)
            {
                FikaServer server = Singleton<FikaServer>.Instance;
                while (server.ReadyClients < expectedPlayers)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                FikaClient client = Singleton<FikaClient>.Instance;
                while (client.ReadyClients < expectedPlayers)
                {
                    yield return new WaitForEndOfFrame();
                }
            }

            if (isServer)
            {
                if (Location_0.OldSpawn && wavesSpawnScenario_0.SpawnWaves != null && wavesSpawnScenario_0.SpawnWaves.Length != 0)
                {
                    Logger.LogInfo("Running old spawn system. Waves: " + wavesSpawnScenario_0.SpawnWaves.Length);
                    if (wavesSpawnScenario_0 != null)
                    {
                        wavesSpawnScenario_0.Run(EBotsSpawnMode.Anyway);
                    }
                }

                if (Location_0.NewSpawn)
                {
                    Logger.LogInfo("Running new spawn system.");
                    if (nonWavesSpawnScenario_0 != null)
                    {
                        nonWavesSpawnScenario_0.Run();
                    }
                }

                GClass579.Run(EBotsSpawnMode.Anyway);

                FikaPlugin.DynamicAI.SettingChanged += DynamicAI_SettingChanged;
                FikaPlugin.DynamicAIRate.SettingChanged += DynamicAIRate_SettingChanged;
            }
            else
            {
                if (wavesSpawnScenario_0 != null)
                {
                    wavesSpawnScenario_0.Stop();
                }
                if (nonWavesSpawnScenario_0 != null)
                {
                    nonWavesSpawnScenario_0.Stop();
                }
                if (GClass579 != null)
                {
                    GClass579.Stop();
                }
            }

            // Add FreeCamController to GameWorld GameObject
            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<FreeCameraController>();
            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<FikaAirdropsManager>();
            FikaAirdropsManager.ContainerCount = 0;

            SetupBorderzones();

            if (Singleton<GameWorld>.Instance.MineManager != null)
            {
                Singleton<GameWorld>.Instance.MineManager.OnExplosion += OnMineExplode;
            }

            Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal = Math.Max(Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal, 5);

            yield return base.vmethod_4(controllerSettings, spawnSystem, runCallback);
            yield break;
        }

        /// <summary>
        /// Triggers when the <see cref="FikaPlugin.DynamicAIRate"/> setting is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DynamicAIRate_SettingChanged(object sender, EventArgs e)
        {
            if (DynamicAI != null)
            {
                DynamicAI.RateChanged(FikaPlugin.DynamicAIRate.Value);
            }
        }

        /// <summary>
        /// Triggers when the <see cref="FikaPlugin.DynamicAI"/> setting is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DynamicAI_SettingChanged(object sender, EventArgs e)
        {
            if (DynamicAI != null)
            {
                DynamicAI.EnabledChange(FikaPlugin.DynamicAI.Value);
            }
        }

        /// <summary>
        /// Sets up all the <see cref="BorderZone"/>s on the map
        /// </summary>
        private void SetupBorderzones()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            gameWorld.BorderZones = LocationScene.GetAllObjects<BorderZone>(false).ToArray();
            for (int i = 0; i < gameWorld.BorderZones.Length; i++)
            {
                gameWorld.BorderZones[i].Id = i;
            }

            if (isServer)
            {
                foreach (BorderZone borderZone in gameWorld.BorderZones)
                {
                    borderZone.PlayerShotEvent += OnBorderZoneShot;
                }
            }
            else
            {
                foreach (BorderZone borderZone in gameWorld.BorderZones)
                {
                    borderZone.RemoveAuthority();
                }
            }
        }

        /// <summary>
        /// Triggered when a <see cref="BorderZone"/> triggers (only runs on host)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="zone"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        private void OnBorderZoneShot(GInterface106 player, BorderZone zone, float arg3, bool arg4)
        {
            BorderZonePacket packet = new()
            {
                ProfileId = player.iPlayer.ProfileId,
                ZoneId = zone.Id
            };
            Singleton<FikaServer>.Instance.SendDataToAll(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Triggers when a <see cref="MineDirectional"/> explodes
        /// </summary>
        /// <param name="directional"></param>
        private void OnMineExplode(MineDirectional directional)
        {
            if (!directional.gameObject.active)
            {
                return;
            }

            MinePacket packet = new()
            {
                MinePositon = directional.transform.position
            };
            if (!isServer)
            {
                Singleton<FikaClient>.Instance.SendData(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Singleton<FikaServer>.Instance.SendDataToAll(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
        }

        /// <summary>
        /// Sets up <see cref="HealthControllerClass"/> events and all <see cref="ExfiltrationPoint"/>s
        /// </summary>
        public override void vmethod_5()
        {
            GameTimer.Start(null, null);
            gparam_0.Player.HealthController.DiedEvent += HealthController_DiedEvent;
            gparam_0.vmethod_0();

            SkillClass[] skills = Profile_0.Skills.Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                skills[i].SetPointsEarnedInSession(0f, false);
            }

            InfiltrationPoint = spawnPoint.Infiltration;
            Profile_0.Info.EntryPoint = InfiltrationPoint;
            Logger.LogInfo("SpawnPoint: " + spawnPoint.Id + ", InfiltrationPoint: " + InfiltrationPoint);

            if (!isServer)
            {
                CarExtraction carExtraction = FindObjectOfType<CarExtraction>();
                if (carExtraction != null)
                {
                    carExtraction.Subscribee.OnStatusChanged -= carExtraction.OnStatusChangedHandler;
                }
            }

            ExfiltrationControllerClass.Instance.InitAllExfiltrationPoints(Location_0.exits, !isServer, "");
            ExfiltrationPoint[] exfilPoints = ExfiltrationControllerClass.Instance.EligiblePoints(Profile_0);

            GameUi.TimerPanel.SetTime(GClass1304.UtcNow, Profile_0.Info.Side, GameTimer.SessionSeconds(), exfilPoints);

            exfilManager = gameObject.AddComponent<CoopExfilManager>();
            exfilManager.Run(exfilPoints);

            if (FikaPlugin.Instance.UseBTR)
            {
                try
                {
                    BackendConfigSettingsClass.BTRGlobalSettings btrSettings = Singleton<BackendConfigSettingsClass>.Instance.BTRSettings;
                    GameWorld gameWorld = Singleton<GameWorld>.Instance;

                    // Only run on maps that have the BTR enabled
                    string location = gameWorld.MainPlayer.Location;
                    if (btrSettings.LocationsWithBTR.Contains(location))
                    {
                        if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                        {
                            if (isServer)
                            {
                                coopHandler.serverBTR = gameWorld.gameObject.AddComponent<FikaBTRManager_Host>();
                            }
                            else
                            {
                                coopHandler.clientBTR = gameWorld.gameObject.AddComponent<FikaBTRManager_Client>();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Logger.LogError("vmethod_5: Exception thrown during BTR init, check logs.");
                }
            }

            dateTime_0 = GClass1304.Now;
            Status = GameStatus.Started;
            ConsoleScreen.ApplyStartCommands();
        }

        /// <summary>
        /// Updates a <see cref="ExfiltrationPoint"/> from the server
        /// </summary>
        /// <param name="point"></param>
        /// <param name="enable"></param>
        public void UpdateExfilPointFromServer(ExfiltrationPoint point, bool enable)
        {
            exfilManager.UpdateExfilPointFromServer(point, enable);
        }

        /// <summary>
        /// Resets all <see cref="ExfiltrationPoint"/>s from the server
        /// </summary>
        /// <param name="points"></param>
        public void ResetExfilPointsFromServer(ExfiltrationPoint[] points)
        {
            Dictionary<string, ExitTimerPanel> currentExfils = Traverse.Create(GameUi.TimerPanel).Field<Dictionary<string, ExitTimerPanel>>("dictionary_0").Value;
            foreach (ExitTimerPanel exitTimerPanel in currentExfils.Values)
            {
                exitTimerPanel.Close();
            }
            currentExfils.Clear();

            GameUi.TimerPanel.SetTime(GClass1304.UtcNow, Profile_0.Info.Side, GameTimer.SessionSeconds(), points);
        }

        /// <summary>
        /// When the local player successfully extracts, enable freecam, notify other players about the extract
        /// </summary>
        /// <param name="player">The local player to start the Coroutine on</param>
        /// <returns></returns>
        public void Extract(CoopPlayer player, ExfiltrationPoint point)
        {
            PreloaderUI preloaderUI = Singleton<PreloaderUI>.Instance;


            if (MyExitStatus == ExitStatus.MissingInAction)
            {
                NotificationManagerClass.DisplayMessageNotification("You have gone missing in action...", iconType: EFT.Communications.ENotificationIconType.Alert, textColor: Color.red);
            }

            BackendConfigSettingsClass.GClass1361.GClass1367 matchEndConfig = Singleton<BackendConfigSettingsClass>.Instance.Experience.MatchEnd;
            if (player.Profile.EftStats.SessionCounters.GetAllInt([CounterTag.Exp]) < matchEndConfig.SurvivedExpRequirement || PastTime < matchEndConfig.SurvivedTimeRequirement)
            {
                MyExitStatus = ExitStatus.Runner;
            }

            if (point != null)
            {
                point.Disable();

                if (point.HasRequirements && point.TransferItemRequirement != null)
                {
                    if (point.TransferItemRequirement.Met(player, point) && player.IsYourPlayer && !HasAddedFenceRep)
                    {
                        player.Profile.EftStats.SessionCounters.AddDouble(0.2, [CounterTag.FenceStanding, EFenceStandingSource.ExitStanding]);
                        HasAddedFenceRep = true;
                    }
                }
            }

            if (player.Side == EPlayerSide.Savage)
            {
                // Seems to already be handled by SPT so we only add it visibly
                player.Profile.EftStats.SessionCounters.AddDouble(0.01, [CounterTag.FenceStanding, EFenceStandingSource.ExitStanding]);
                //player.Profile.FenceInfo.AddStanding(0.1, EFenceStandingSource.ExitStanding);
            }

            GenericPacket genericPacket = new()
            {
                NetId = player.NetId,
                PacketType = EPackageType.ClientExtract
            };

            try // This is to allow clients to extract if they lose connection
            {
                if (!isServer)
                {
                    Singleton<FikaClient>.Instance.SendData(new NetDataWriter(), ref genericPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                }
                else
                {
                    Singleton<FikaServer>.Instance.SendDataToAll(new NetDataWriter(), ref genericPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    ClearHostAI(player);
                }
            }
            catch
            {

            }

            CoopHandler coopHandler = CoopHandler.GetCoopHandler();

            CoopPlayer coopPlayer = player;
            ExtractedPlayers.Add(coopPlayer.NetId);
            coopHandler.ExtractedPlayers.Add(coopPlayer.NetId);
            coopHandler.Players.Remove(coopPlayer.NetId);

            preloaderUI.StartBlackScreenShow(2f, 2f, () => { preloaderUI.FadeBlackScreen(2f, -2f); });

            player.ActiveHealthController.SetDamageCoeff(0);
            player.ActiveHealthController.AddDamageMultiplier(0);
            player.ActiveHealthController.DisableMetabolism();
            player.ActiveHealthController.PauseAllEffects();

            extractRoutine = StartCoroutine(ExtractRoutine(player));

            // Prevents players from looting after extracting
            GClass3131.Instance.CloseAllScreensForced();

            // Detroys session timer
            if (timeManager != null)
            {
                Destroy(timeManager);
            }
            if (GameUi.TimerPanel.enabled)
            {
                GameUi.TimerPanel.Close();
            }

            if (FikaPlugin.AutoExtract.Value)
            {
                if (!isServer)
                {
                    Stop(coopHandler.MyPlayer.ProfileId, MyExitStatus, coopHandler.MyPlayer.ActiveHealthController.IsAlive ? MyExitLocation : null, 0);
                }
                else if (Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount == 0)
                {
                    Stop(coopHandler.MyPlayer.ProfileId, MyExitStatus, coopHandler.MyPlayer.ActiveHealthController.IsAlive ? MyExitLocation : null, 0);
                }
            }
        }

        /// <summary>
        /// Used to make sure no stims or mods reset the DamageCoeff
        /// </summary>
        /// <param name="player">The <see cref="CoopPlayer"/> to run the coroutine on</param>
        /// <returns></returns>
        private IEnumerator ExtractRoutine(CoopPlayer player)
        {
            while (true)
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

        public void ClearHostAI(Player player)
        {
            if (player != null)
            {
                if (botsController_0 != null)
                {
                    foreach (KeyValuePair<BotZone, GClass491> kvp in botsController_0.Groups())
                    {
                        foreach (BotsGroup botsGroup in kvp.Value.GetGroups(true))
                        {
                            botsGroup.RemoveEnemy(player);
                            botsGroup.AddAlly(player);
                        }
                    }

                    BotsClass bots = Traverse.Create(botsController_0.BotSpawner).Field<BotsClass>("_bots").Value;
                    HashSet<BotOwner> allBots = Traverse.Create(bots).Field<HashSet<BotOwner>>("hashSet_0").Value;

                    foreach (BotOwner bot in allBots)
                    {
                        bot.Memory.DeleteInfoAboutEnemy(player);
                    }
                }
            }
        }

        /// <summary>
        /// Triggers when the main player dies
        /// </summary>
        /// <param name="obj"></param>
        private void HealthController_DiedEvent(EDamageType obj)
        {
            if (timeManager != null)
            {
                Destroy(timeManager);
            }
            if (GameUi.TimerPanel != null && GameUi.TimerPanel.enabled)
            {
                GameUi.TimerPanel.Close();
            }

            gparam_0.Player.HealthController.DiedEvent -= method_15;
            gparam_0.Player.HealthController.DiedEvent -= HealthController_DiedEvent;

            PlayerOwner.vmethod_1();
            MyExitStatus = ExitStatus.Killed;
            MyExitLocation = null;

            if (FikaPlugin.Instance.ForceSaveOnDeath)
            {
                StartCoroutine(SaveOnDeathRoutine());
            }
        }

        private IEnumerator SaveOnDeathRoutine()
        {
            Task saveTask = SavePlayer((CoopPlayer)gparam_0.Player, MyExitStatus, null, true);
            while (!saveTask.IsCompleted)
            {
                yield return null;
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
            Logger.LogDebug("Stop");

            CoopPlayer myPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            myPlayer.PacketSender.DestroyThis();

            if (myPlayer.Side != EPlayerSide.Savage)
            {
                if (myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem != null)
                {
                    GStruct414<GClass2801> result = InteractionsHandlerClass.Remove(myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem,
                        myPlayer.GClass2777_0, false, true);
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

            if (isServer)
            {
                botsController_0.Stop();
                botsController_0.DestroyInfo(gparam_0.Player);
            }

            if (GClass579 != null)
            {
                GClass579.Stop();
            }
            if (nonWavesSpawnScenario_0 != null)
            {
                nonWavesSpawnScenario_0.Stop();
            }
            if (wavesSpawnScenario_0 != null)
            {
                wavesSpawnScenario_0.Stop();
            }

            try
            {
                PlayerLeftRequest body = new(myPlayer.ProfileId);
                FikaRequestHandler.RaidLeave(body);
            }
            catch (Exception)
            {
                Logger.LogError("Unable to send RaidLeave request to server.");
            }

            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                // Create a copy to prevent errors when the dictionary is being modified (which happens when using spawn mods)
                CoopPlayer[] players = [.. coopHandler.Players.Values];
                foreach (CoopPlayer player in players)
                {
                    if (player == null)
                    {
                        continue;
                    }

                    if (player.IsYourPlayer)
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

            coopHandler.RunAsyncTasks = false;
            Destroy(coopHandler);

            if (CoopHandler.CoopHandlerParent != null)
            {
                Destroy(CoopHandler.CoopHandlerParent);
            }

            ExitManager stopManager = new(this, exitStatus, exitName, delay, myPlayer);

            GameUI gameUI = GameUI.Instance;

            exfilManager.Stop();

            Status = GameStatus.Stopping;
            GameTimer.TryStop();
            if (gameUI.TimerPanel.enabled)
            {
                gameUI.TimerPanel.Close();
            }
            if (EnvironmentManager.Instance != null)
            {
                EnvironmentManager.Instance.Stop();
            }
            MonoBehaviourSingleton<PreloaderUI>.Instance.StartBlackScreenShow(1f, 1f, new Action(stopManager.HandleExit));
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
        private Task SavePlayer(CoopPlayer player, ExitStatus exitStatus, string exitName, bool fromDeath)
        {
            if (hasSaved)
            {
                return Task.CompletedTask;
            }

            if (fromDeath)
            {
                //Since we're bypassing saving on exiting, run this now.
                player.Profile.EftStats.LastPlayerState = null;
                player.StatisticsManager.EndStatisticsSession(exitStatus, PastTime);
                player.CheckAndResetControllers(exitStatus, PastTime, Location_0.Id, exitName);
            }

            //Method taken directly from SPT, can be found in the aki-singleplayer assembly as OfflineSaveProfilePatch
            Type converterClass = typeof(AbstractGame).Assembly.GetTypes().First(t => t.GetField("Converters", BindingFlags.Static | BindingFlags.Public) != null);

            JsonConverter[] Converters = Traverse.Create(converterClass).Field<JsonConverter[]>("Converters").Value;

            SaveProfileRequest SaveRequest = new()
            {
                Exit = exitStatus.ToString().ToLowerInvariant(),
                Profile = player.Profile,
                Health = HealthListener.Instance.CurrentHealth,
                Insurance = InsuredItemManager.Instance.GetTrackedItems(),
                IsPlayerScav = player.Side is EPlayerSide.Savage
            };

            RequestHandler.PutJson("/raid/profile/save", SaveRequest.ToJson(Converters.AddItem(new NotesJsonConverter()).ToArray()));

            hasSaved = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the local <see cref="CoopGame"/> when waiting for other players
        /// </summary>
        /// <param name="profileId"></param>
        /// <param name="exitStatus"></param>
        private void StopFromCancel(string profileId, ExitStatus exitStatus)
        {
            Logger.LogWarning("Game init was cancelled!");

            CoopPlayer myPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            myPlayer.PacketSender.DestroyThis();

            if (myPlayer.Side != EPlayerSide.Savage)
            {
                if (myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem != null)
                {
                    GStruct414<GClass2801> result = InteractionsHandlerClass.Remove(myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem,
                        myPlayer.GClass2777_0, false, true);
                    if (result.Error != null)
                    {
                        Logger.LogWarning("StopFromError: Error removing dog tag!");
                    }
                }
            }

            string exitName = null;
            float delay = 0f;

            PlayerLeftRequest body = new(profileId);
            FikaRequestHandler.RaidLeave(body);

            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                foreach (CoopPlayer player in coopHandler.Players.Values)
                {
                    if (player == null)
                    {
                        continue;
                    }

                    player.Dispose();
                    AssetPoolObject.ReturnToPool(player.gameObject, true);
                }
                coopHandler.Players.Clear();
            }
            else
            {
                Logger.LogError("Stop: Could not find CoopHandler!");
            }

            coopHandler.RunAsyncTasks = false;
            Destroy(coopHandler);

            if (CoopHandler.CoopHandlerParent != null)
            {
                Destroy(CoopHandler.CoopHandlerParent);
            }

            if (GClass579 != null)
            {
                GClass579.Stop();
            }
            if (nonWavesSpawnScenario_0 != null)
            {
                nonWavesSpawnScenario_0.Stop();
            }
            if (wavesSpawnScenario_0 != null)
            {
                wavesSpawnScenario_0.Stop();
            }

            CancelExitManager stopManager = new()
            {
                baseLocalGame_0 = this,
                exitStatus = exitStatus,
                exitName = exitName,
                delay = delay
            };

            GameUI gameUI = GameUI.Instance;

            if (exfilManager != null)
            {
                exfilManager.Stop();
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
            MonoBehaviourSingleton<PreloaderUI>.Instance.StartBlackScreenShow(1f, 1f, new Action(stopManager.ExitOverride));
            BackendConfigAbstractClass.Config.UseSpiritPlayer = false;
        }

        /// <summary>
        /// Toggles the <see cref="FikaDebug"/> menu
        /// </summary>
        /// <param name="enabled"></param>
        public void ToggleDebug(bool enabled)
        {
            if (fikaDebug != null)
            {
                fikaDebug.enabled = enabled;
            }
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
                    Debug.LogException(ex);
                }
            }
            dictionary_0.Clear();
            // Reset MatchingType to Single when the game ends.
            FikaBackendUtils.MatchingType = EMatchmakerType.Single;
        }

        /// <summary>
        /// Disposes of the <see cref="CoopGame"/>
        /// </summary>
        public override void Dispose()
        {
            Logger.LogDebug("Dispose()");

            if (Singleton<GameWorld>.Instance.MineManager != null)
            {
                Singleton<GameWorld>.Instance.MineManager.OnExplosion -= OnMineExplode;
            }

            if (extractRoutine != null)
            {
                StopCoroutine(extractRoutine);
            }

            if (isServer)
            {
                CoopPlayer coopPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
                coopPlayer.PacketSender.DestroyThis();

                FikaDynamicAI newDynamicAI = gameObject.GetComponent<FikaDynamicAI>();
                if (newDynamicAI != null)
                {
                    Destroy(newDynamicAI);
                }

                NetManagerUtils.StopPinger();

                FikaPlugin.DynamicAI.SettingChanged -= DynamicAI_SettingChanged;
                FikaPlugin.DynamicAIRate.SettingChanged -= DynamicAIRate_SettingChanged;
            }
            else
            {
                // Resetting this array to null forces the game to re-allocate it if the client hosts the next session
                typeof(BotsController).GetField("_allTypes", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(botsController_0, null);
            }

            NetManagerUtils.DestroyNetManager(isServer);

            FikaBackendUtils.Nodes = null;
            FikaBackendUtils.HostExpectedNumberOfPlayers = 1;

            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                coopHandler.RunAsyncTasks = false;
                Destroy(coopHandler);

                if (CoopHandler.CoopHandlerParent != null)
                {
                    Destroy(CoopHandler.CoopHandlerParent);
                }
            }

            if (Singleton<FikaAirdropsManager>.Instance != null)
            {
                Destroy(Singleton<FikaAirdropsManager>.Instance);
            }

            base.Dispose();
        }

        /// <summary>
        /// Used to manage the stopping of the <see cref="CoopGame"/> gracefully
        /// </summary>
        /// <param name="localGame"></param>
        /// <param name="exitStatus"></param>
        /// <param name="exitName"></param>
        /// <param name="delay"></param>
        /// <param name="localPlayer"></param>
        private class ExitManager(CoopGame localGame, ExitStatus exitStatus, string exitName, float delay, CoopPlayer localPlayer)
        {
            private readonly CoopGame localGame = localGame;
            private readonly ExitStatus exitStatus = exitStatus;
            private readonly string exitName = exitName;
            private readonly float delay = delay;
            private readonly CoopPlayer localPlayer = localPlayer;
            private Action EndAction;

            public void HandleExit()
            {
                GClass3131 screenManager = GClass3131.Instance;
                if (screenManager.CheckCurrentScreen(EEftScreenType.Reconnect))
                {
                    screenManager.CloseAllScreensForced();
                }
                localGame.gparam_0.Player.OnGameSessionEnd(exitStatus, localGame.PastTime, localGame.Location_0.Id, exitName);
                localGame.CleanUp();
                localGame.Status = GameStatus.Stopped;
                TimeSpan timeSpan = GClass1304.Now - localGame.dateTime_0;
                localGame.ginterface158_0.OfflineRaidEnded(exitStatus, exitName, timeSpan.TotalSeconds).HandleExceptions();
                MonoBehaviourSingleton<BetterAudio>.Instance.FadeOutVolumeAfterRaid();
                StaticManager staticManager = StaticManager.Instance;
                float num = delay;
                EndAction = new Action(FireCallback);
                staticManager.WaitSeconds(num, EndAction);
            }

            private void FireCallback()
            {
                Callback<ExitStatus, TimeSpan, MetricsClass> endCallback = Traverse.Create(localGame).Field<Callback<ExitStatus, TimeSpan, MetricsClass>>("callback_0").Value;

                localGame.SavePlayer(localPlayer, exitStatus, exitName, false);

                endCallback(new Result<ExitStatus, TimeSpan, MetricsClass>(exitStatus, GClass1304.Now - localGame.dateTime_0, new MetricsClass()));
                UIEventSystem.Instance.Enable();
            }
        }

        /// <summary>
        /// Used to manage the stopping of the <see cref="CoopGame"/> gracefully when cancelling
        /// </summary>
        private class CancelExitManager : Class1386
        {
            public void ExitOverride()
            {
                GClass3131 instance = GClass3131.Instance;
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
                MonoBehaviour instance2 = StaticManager.Instance;
                float num = delay;
                action_0 = new Action(method_1);
                instance2.WaitSeconds(num, action_0);
            }
        }

        public new void method_6(string backendUrl, string locationId, int variantId)
        {
            Logger.LogDebug("method_6");
            return;
        }
    }
}
