using Aki.Common.Http;
using Aki.Custom.Airdrops;
using Aki.Reflection.Utils;
using Aki.SinglePlayer.Models.Progression;
using Aki.SinglePlayer.Utils.Healing;
using Aki.SinglePlayer.Utils.Insurance;
using Aki.SinglePlayer.Utils.Progression;
using BepInEx.Logging;
using Comfort.Common;
using CommonAssets.Scripts.Game;
using EFT;
using EFT.AssetsManager;
using EFT.Bots;
using EFT.CameraControl;
using EFT.Counters;
using EFT.EnvironmentEffect;
using EFT.Game.Spawning;
using EFT.InputSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.BattleTimer;
using EFT.UI.Screens;
using EFT.Weather;
using Fika.Core.Coop.BTR;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.FreeCamera;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Coop.Players;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using Fika.Core.Networking.Packets.GameWorld;
using Fika.Core.UI.Models;
using HarmonyLib;
using JsonType;
using LiteNetLib.Utils;
using Newtonsoft.Json;
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
    /// A custom Game Type
    /// </summary>
    internal sealed class CoopGame : BaseLocalGame<GamePlayerOwner>, IBotGame, IFikaGame
    {
        public string InfiltrationPoint;
        public bool HasAddedFenceRep = false;
        public bool forceStart = false;
        public ExitStatus MyExitStatus { get; set; } = ExitStatus.Survived;
        public string MyExitLocation { get; set; } = null;
        public ISpawnSystem SpawnSystem;

        public Dictionary<string, Player> Bots = [];
        private CoopExfilManager exfilManager;
        private GameObject fikaStartButton;
        private readonly Dictionary<int, int> botQueue = [];
        private Coroutine extractRoutine;
        private GClass2928 spawnPoints = null;
        private ISpawnPoint spawnPoint = null;
        private GClass579 GClass579;
        private WavesSpawnScenario wavesSpawnScenario_0;
        private NonWavesSpawnScenario nonWavesSpawnScenario_0;
        private Func<Player, GamePlayerOwner> func_1;
        private bool hasSaved = false;

        public FikaDynamicAI DynamicAI { get; private set; }
        public RaidSettings RaidSettings { get; private set; }
        //WildSpawnType for sptUsec and sptBear
        const int sptUsecValue = 47;
        const int sptBearValue = 48;
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

        internal static CoopGame Create(InputTree inputTree, Profile profile, GameDateTime backendDateTime, InsuranceCompanyClass insurance, MenuUI menuUI,
            CommonUI commonUI, PreloaderUI preloaderUI, GameUI gameUI, LocationSettingsClass.Location location, TimeAndWeatherSettings timeAndWeather,
            WavesSettings wavesSettings, EDateTime dateTime, Callback<ExitStatus, TimeSpan, MetricsClass> callback, float fixedDeltaTime, EUpdateQueue updateQueue,
            ISession backEndSession, TimeSpan sessionTime, RaidSettings raidSettings)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource("Coop Game Mode");
            Logger.LogInfo("CoopGame::Create");

            bool useCustomWeather = timeAndWeather.IsRandomWeather;
            timeAndWeather.IsRandomWeather = false;

            CoopGame coopGame = smethod_0<CoopGame>(inputTree, profile, backendDateTime, insurance, menuUI, commonUI,
                preloaderUI, gameUI, location, timeAndWeather, wavesSettings, dateTime, callback, fixedDeltaTime, updateQueue, backEndSession, new TimeSpan?(sessionTime));

            // Non Waves Scenario setup
            coopGame.nonWavesSpawnScenario_0 = NonWavesSpawnScenario.smethod_0(coopGame, location, coopGame.botsController_0);
            coopGame.nonWavesSpawnScenario_0.ImplementWaveSettings(wavesSettings);

            // Waves Scenario setup
            WildSpawnWave[] waves = LocalGame.smethod_7(wavesSettings, location.waves);
            coopGame.wavesSpawnScenario_0 = WavesSpawnScenario.smethod_0(coopGame.gameObject, waves, new Action<BotWaveDataClass>(coopGame.botsController_0.ActivateBotsByWave), location);

            BossLocationSpawn[] bossSpawns = LocalGame.smethod_8(wavesSettings, location.BossLocationSpawn);
            coopGame.GClass579 = GClass579.smethod_0(bossSpawns, new Action<BossLocationSpawn>(coopGame.botsController_0.ActivateBotsByWave));

            if (useCustomWeather && MatchmakerAcceptPatches.IsServer)
            {
                Logger.LogInfo("Custom weather enabled, initializing curves");
                coopGame.SetupCustomWeather(timeAndWeather);
            }

            coopGame.func_1 = (Player player) => GamePlayerOwner.Create<GamePlayerOwner>(player, inputTree, insurance, backEndSession, commonUI, preloaderUI, gameUI, coopGame.GameDateTime, location);

            Singleton<IFikaGame>.Create(coopGame);
            FikaEventDispatcher.DispatchEvent(new FikaGameCreatedEvent(coopGame));

            coopGame.RaidSettings = raidSettings;

            return coopGame;
        }

        private void SetupCustomWeather(TimeAndWeatherSettings timeAndWeather)
        {
            if (WeatherController.Instance == null)
            {
                return;
            }

            DateTime dateTime = GClass1296.StartOfDay();
            DateTime dateTime2 = dateTime.AddDays(1);

            WeatherClass weather = WeatherClass.CreateDefault();
            WeatherClass weather2 = WeatherClass.CreateDefault();
            weather.Cloudness = weather2.Cloudness = timeAndWeather.CloudinessType.ToValue();
            weather.Rain = weather2.Rain = timeAndWeather.RainType.ToValue();
            weather.Wind = weather2.Wind = timeAndWeather.WindType.ToValue();
            weather.ScaterringFogDensity = weather2.ScaterringFogDensity = timeAndWeather.FogType.ToValue();
            weather.Time = dateTime.Ticks;
            weather2.Time = dateTime2.Ticks;
            WeatherController.Instance.method_4([weather, weather2]);
        }

        public async Task CreateCoopHandler()
        {
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

            if (!string.IsNullOrEmpty(MatchmakerAcceptPatches.GetGroupId()))
            {
                coopHandler.ServerId = MatchmakerAcceptPatches.GetGroupId();
            }
            else
            {
                Destroy(coopHandler);
                Logger.LogError("No Server Id found, Deleting Coop Handler");
                throw new Exception("No Server Id found");
            }

            if (MatchmakerAcceptPatches.IsServer)
            {
                FikaServer server = gameObject.AddComponent<FikaServer>();

                while (!server.ServerReady)
                {
                    await Task.Delay(100);
                }
                Logger.LogInfo("FikaServer has started!");
            }
            else if (MatchmakerAcceptPatches.IsClient)
            {
                FikaClient client = gameObject.AddComponent<FikaClient>();

                while (!client.ClientReady)
                {
                    await Task.Delay(100);
                }
                Logger.LogInfo("FikaClient has started!");
            }
        }

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

        private string GetFurthestBot(Dictionary<string, Player> bots, CoopHandler coopHandler, out float furthestDistance)
        {
            string furthestBot = string.Empty;
            furthestDistance = 0f;

            List<CoopPlayer> humanPlayers = GetPlayers(coopHandler);

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

            if ((int)role != sptUsecValue && (int)role != sptBearValue && role != WildSpawnType.assault)
            {
                // We skip all the bots that are not sptUsec, sptBear or assault. That means we never remove bosses, bossfollowers, and raiders
                return true;
            }

            return false;
        }

        private async Task<LocalPlayer> CreateBot(Profile profile, Vector3 position)
        {
#if DEBUG
            Logger.LogWarning($"Creating bot {profile.Info.Settings.Role} at {position}");
#endif
            if (MatchmakerAcceptPatches.IsClient)
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
            if ((int)role != sptUsecValue && (int)role != sptBearValue && role != WildSpawnType.assault)
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
                    despawned = TryDespawnFurthest(profile, position, coopHandler);
                }

                // If it's not special and we didnt despawn something, we dont spawn a new bot.
                if (!isSpecial && !despawned)
                {
#if DEBUG
                    Logger.LogWarning($"Stopping spawn of bot {profile.Nickname}, max count reached and enforced limits enabled. Current: {botsController_0.AliveAndLoadingBotsCount}, Max: {botsController_0.BotSpawner.MaxBots}");
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

                SendCharacterPacket packet = new(new FikaSerialization.PlayerInfoPacket() { Profile = profile }, true, true, position, netId);
                Singleton<FikaServer>.Instance.SendDataToAll(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableUnordered);

                if (server.NetServer.ConnectedPeersCount > 0)
                {
                    await WaitForPlayersToLoadBotProfile(netId);
                }

                localPlayer = await CoopBot.CreateBot(num, position, Quaternion.identity, "Player",
                   "Bot_", EPointOfView.ThirdPerson, profile, true, UpdateQueue, Player.EUpdateMode.Manual,
                   Player.EUpdateMode.Auto, GClass549.Config.CharacterController.BotPlayerMode, () => 1f,
                   () => 1f, GClass1446.Default);

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

                if (profile.Info.Side == EPlayerSide.Bear || profile.Info.Side == EPlayerSide.Usec)
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
            coopHandler.Players.Add(coopBot.NetId, coopBot);

            return localPlayer;
        }

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

        private bool TryDespawnFurthest(Profile profile, Vector3 position, CoopHandler coopHandler)
        {
            string botKey = GetFurthestBot(Bots, coopHandler, out float furthestDistance);

            if (botKey == string.Empty)
            {
#if DEBUG
                Logger.LogWarning("TryDespawnFurthest: botKey was empty"); 
#endif
                return false;
            }

            if (furthestDistance > GetDistanceFromPlayers(position, GetPlayers(coopHandler)))
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
            Logger.LogWarning($"Removing {bot.Profile.Info.Settings.Role} at a distance of {Math.Sqrt(furthestDistance)}m from ITs nearest player.");
#endif
            DespawnBot(coopHandler, bot);
#if DEBUG
            Logger.LogWarning($"Bot {bot.Profile.Info.Settings.Role} despawned successfully.");
#endif
            return true;
        }

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
        /// We use <see cref="DeployScreen(float)"/> instead
        /// </summary>
        /// <param name="timeBeforeDeploy"></param>
        public override void vmethod_1(float timeBeforeDeploy)
        {
            /// Do nothing
        }

        /// <summary>
        /// Matchmaker countdown
        /// </summary>
        /// <param name="timeBeforeDeploy">Time in seconds to count down</param>
        private async void DeployScreen(float timeBeforeDeploy)
        {
            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                if (MatchmakerAcceptPatches.IsServer && MatchmakerAcceptPatches.HostExpectedNumberOfPlayers <= 1)
                {
                    if (fikaStartButton != null)
                    {
                        Destroy(fikaStartButton);
                    }

                    if (DynamicAI != null)
                    {
                        DynamicAI.AddHumans();
                    }

                    SetStatusModel status = new(coopHandler.MyPlayer.ProfileId, LobbyEntry.ELobbyStatus.IN_GAME);
                    await FikaRequestHandler.UpdateSetStatus(status);

                    Singleton<FikaServer>.Instance.ReadyClients++;
                    base.vmethod_1(timeBeforeDeploy);
                    return;
                }

                NetDataWriter writer = new();
                forceStart = false;

                MatchmakerAcceptPatches.GClass3163.ChangeStatus("Waiting for other players to finish loading...");

                if (fikaStartButton != null)
                {
                    fikaStartButton.SetActive(true);
                }

                if (MatchmakerAcceptPatches.IsServer)
                {
                    SetStatusModel status = new(coopHandler.MyPlayer.ProfileId, LobbyEntry.ELobbyStatus.IN_GAME);
                    await FikaRequestHandler.UpdateSetStatus(status);

                    do
                    {
                        await Task.Delay(100);
                    } while (coopHandler.HumanPlayers < MatchmakerAcceptPatches.HostExpectedNumberOfPlayers && !forceStart);

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
                        await Task.Delay(250);
                    } while (Singleton<FikaServer>.Instance.ReadyClients < MatchmakerAcceptPatches.HostExpectedNumberOfPlayers && !forceStart);

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
                else if (MatchmakerAcceptPatches.IsClient)
                {
                    do
                    {
                        await Task.Delay(100);
                    } while (coopHandler.HumanPlayers < MatchmakerAcceptPatches.HostExpectedNumberOfPlayers && !forceStart);

                    FikaClient client = Singleton<FikaClient>.Instance;
                    InformationPacket packet = new(true)
                    {
                        ReadyPlayers = 1
                    };
                    writer.Reset();
                    client.SendData(writer, ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);

                    do
                    {
                        await Task.Delay(250);
                    } while (Singleton<FikaClient>.Instance.ReadyClients < MatchmakerAcceptPatches.HostExpectedNumberOfPlayers && !forceStart);
                }

                if (fikaStartButton != null)
                {
                    Destroy(fikaStartButton);
                }

            }

            base.vmethod_1(timeBeforeDeploy);
        }

        private async Task SendOrReceiveSpawnPoint()
        {
            if (MatchmakerAcceptPatches.IsServer)
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
            else if (MatchmakerAcceptPatches.IsClient)
            {
                SpawnPointRequest body = new();
                SpawnPointResponse response = await FikaRequestHandler.RaidSpawnPoint(body);
                string name = response.SpawnPoint;

                if (!string.IsNullOrEmpty(name))
                {
                    Logger.LogInfo($"Retrieved Spawn Point '{name}' from server");

                    Dictionary<ISpawnPoint, SpawnPointMarker> allSpawnPoints = Traverse.Create(spawnPoints).Field("dictionary_0").GetValue<Dictionary<ISpawnPoint, SpawnPointMarker>>();
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
        /// Creating the EFT.LocalPlayer
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
        /// <returns></returns>
        public override async Task<LocalPlayer> vmethod_2(int playerId, Vector3 position, Quaternion rotation,
            string layerName, string prefix, EPointOfView pointOfView, Profile profile, bool aiControl,
            EUpdateQueue updateQueue, Player.EUpdateMode armsUpdateMode, Player.EUpdateMode bodyUpdateMode,
            CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity, Func<float> getAimingSensitivity,
            IStatisticsManager statisticsManager, AbstractQuestControllerClass questController, AbstractAchievementControllerClass achievementsController)
        {
            Logger.LogInfo("Creating CoopHandler");
            await CreateCoopHandler();
            CoopHandler.GetCoopHandler().LocalGameInstance = this;

            LocalPlayer myPlayer = await CoopPlayer.Create(playerId, spawnPoint.Position, spawnPoint.Rotation, "Player", "Main_", EPointOfView.FirstPerson, profile,
                false, UpdateQueue, armsUpdateMode, bodyUpdateMode,
                GClass549.Config.CharacterController.ClientPlayerMode, getSensitivity,
                getAimingSensitivity, new GClass1445(), MatchmakerAcceptPatches.IsServer ? 0 : 1000, statisticsManager);

            profile.SetSpawnedInSession(profile.Side == EPlayerSide.Savage);

            if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                Logger.LogDebug($"{nameof(vmethod_2)}:Unable to find {nameof(CoopHandler)}");
                throw new MissingComponentException("CoopHandler was missing during CoopGame init");
            }

            if (RaidSettings.MetabolismDisabled)
            {
                myPlayer.HealthController.DisableMetabolism();
                NotificationManagerClass.DisplayMessageNotification("Metabolism disabled", iconType: EFT.Communications.ENotificationIconType.Alert);
            }

            CoopPlayer coopPlayer = (CoopPlayer)myPlayer;
            coopHandler.Players.Add(coopPlayer.NetId, coopPlayer);

            PlayerSpawnRequest body = new(myPlayer.ProfileId, MatchmakerAcceptPatches.GetGroupId());
            await FikaRequestHandler.UpdatePlayerSpawn(body);

            myPlayer.SpawnPoint = spawnPoint;

            GameObject customButton = null;
            GameObject customButtonStart = null;

            // This creates a "custom" Back button so that we can back out if we get stuck
            if (MenuUI.Instantiated)
            {
                MenuUI menuUI = MenuUI.Instance;
                DefaultUIButton backButton = Traverse.Create(menuUI.MatchmakerTimeHasCome).Field("_cancelButton").GetValue<DefaultUIButton>();
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
                    Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("WARNING", message: "Backing out from this stage is currently experimental. It is recommended to ALT+F4 instead. Do you still want to continue?",
                        ErrorScreen.EButtonType.OkButton, 15f, () =>
                        {
                            StopFromError(myPlayer.ProfileId, ExitStatus.Runner);
                            PlayerLeftRequest playerLeftRequest = new(coopPlayer.ProfileId);
                            FikaRequestHandler.RaidLeave(playerLeftRequest);
                        }, null);
                });
                Traverse.Create(backButtonComponent).Field("OnClick").SetValue(newEvent);

                if (MatchmakerAcceptPatches.IsServer)
                {
                    DefaultUIButton startButton = Traverse.Create(menuUI.MatchmakerTimeHasCome).Field("_cancelButton").GetValue<DefaultUIButton>();
                    customButtonStart = Instantiate(backButton.gameObject, backButton.gameObject.transform.parent);
                    customButtonStart.gameObject.name = "FikaStartButton";
                    customButtonStart.gameObject.SetActive(true);
                    customButtonStart.gameObject.transform.position = new(customButton.transform.position.x, customButton.transform.position.y + 60, customButton.transform.position.z);
                    DefaultUIButton startButtonComponent = customButtonStart.GetComponent<DefaultUIButton>();
                    startButtonComponent.SetHeaderText("Force Start", 32);
                    startButtonComponent.SetEnabledTooltip("EXPERIMENTAL: Force starts the game. Use at own risk!");
                    UnityEngine.Events.UnityEvent newStartEvent = new();
                    newStartEvent.AddListener(() =>
                    {
                        forceStart = true;

                        InformationPacket packet = new(false)
                        {
                            ForceStart = true
                        };

                        FikaPlugin.Instance.FikaLogger.LogWarning("Force start was used!");

                        NetDataWriter writer = new();
                        writer.Reset();
                        Singleton<FikaServer>.Instance.SendDataToAll(writer, ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);

                        if (fikaStartButton != null)
                        {
                            fikaStartButton.SetActive(false);
                        }
                    });
                    Traverse.Create(startButtonComponent).Field("OnClick").SetValue(newStartEvent);
                    if (customButton != null)
                    {
                        customButton.SetActive(true);
                    }
                    fikaStartButton = customButtonStart;
                }
            }

            SendCharacterPacket packet = new(new FikaSerialization.PlayerInfoPacket() { Profile = myPlayer.Profile }, myPlayer.HealthController.IsAlive, false, myPlayer.Transform.position, (myPlayer as CoopPlayer).NetId);

            if (MatchmakerAcceptPatches.IsServer)
            {
                await SetStatus(myPlayer, LobbyEntry.ELobbyStatus.COMPLETE);
            }
            else
            {
                Singleton<FikaClient>.Instance.SendData(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableUnordered);
            }

            if (MatchmakerAcceptPatches.IsServer)
            {
                while (!Singleton<FikaServer>.Instantiated && !Singleton<FikaServer>.Instance.ServerReady)
                {
                    await Task.Delay(100);
                }
            }
            else if (MatchmakerAcceptPatches.IsClient)
            {
                while (!Singleton<FikaClient>.Instantiated && !Singleton<FikaClient>.Instance.ClientReady)
                {
                    await Task.Delay(100);
                }
            }

            await WaitForPlayers();

            Destroy(customButton);
            if (fikaStartButton != null)
            {
                fikaStartButton.SetActive(false);
            }

            myPlayer.ActiveHealthController.DiedEvent += MainPlayerDied;

            return myPlayer;
        }

        private void MainPlayerDied(EDamageType obj)
        {
            EndByTimerScenario endByTimerScenario = GetComponent<EndByTimerScenario>();
            if (endByTimerScenario != null)
            {
                Destroy(endByTimerScenario);
            }
            if (GameUi.TimerPanel.enabled)
            {
                GameUi.TimerPanel.Close();
            }
        }

        public async Task InitPlayer(BotControllerSettings botsSettings, string backendUrl, InventoryControllerClass inventoryController, Callback runCallback)
        {
            Status = GameStatus.Running;
            UnityEngine.Random.InitState((int)GClass1296.Now.Ticks);

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
                    location = await ginterface145_0.LoadLocationLoot(Location_0.Id, num);
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
                    Logger.LogError("Can't find event prefab in resources. Path : Prefabs/HALLOWEEN_CONTROLLER");
                }
            }
            GClass785 config = GClass549.Config;
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

        private class StartHandler(BaseLocalGame<GamePlayerOwner> localGame, BotControllerSettings botSettings, ISpawnSystem spawnSystem, Callback runCallback)
        {
            private readonly BaseLocalGame<GamePlayerOwner> localGame = localGame;
            private readonly BotControllerSettings botSettings = botSettings;
            private readonly ISpawnSystem spawnSystem = spawnSystem;
            private readonly Callback runCallback = runCallback;

            public void FinishLoading()
            {
                localGame.method_5(botSettings, spawnSystem, runCallback);
            }
        }

        private async Task<Player> CreateLocalPlayer()
        {
            int num = method_12();

            Player.EUpdateMode eupdateMode = Player.EUpdateMode.Auto;
            if (GClass549.Config.UseHandsFastAnimator)
            {
                eupdateMode = Player.EUpdateMode.Manual;
            }

            spawnPoints = GClass2928.CreateFromScene(new DateTime?(GClass1296.LocalDateTimeFromUnixTime(Location_0.UnixDateTime)), Location_0.SpawnPointParams);
            int spawnSafeDistance = (Location_0.SpawnSafeDistanceMeters > 0) ? Location_0.SpawnSafeDistanceMeters : 100;
            GStruct380 settings = new(Location_0.MinDistToFreePoint, Location_0.MaxDistToFreePoint, Location_0.MaxBotPerZone, spawnSafeDistance);
            SpawnSystem = GClass2929.CreateSpawnSystem(settings, () => Time.time, Singleton<GameWorld>.Instance, zones: botsController_0, spawnPoints);

            if (MatchmakerAcceptPatches.IsServer)
            {
                spawnPoint = SpawnSystem.SelectSpawnPoint(ESpawnCategory.Player, Profile_0.Info.Side);
                await SendOrReceiveSpawnPoint();
            }

            if (MatchmakerAcceptPatches.IsClient)
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
                UpdateQueue, eupdateMode, Player.EUpdateMode.Auto, GClass549.Config.CharacterController.ClientPlayerMode,
                new Func<float>(Class1362.class1362_0.method_1), new Func<float>(Class1362.class1362_0.method_2),
                statisticsManager, null, null);

            myPlayer.Location = Location_0.Id;
            myPlayer.OnEpInteraction += OnEpInteraction;

            return myPlayer;
        }

        private async Task WaitForPlayers()
        {
            Logger.LogInfo("Starting task to wait for other players.");

            if (MatchmakerAcceptPatches.GClass3163 != null)
            {
                MatchmakerAcceptPatches.GClass3163.ChangeStatus($"Initializing Coop Game...");
            }
            int numbersOfPlayersToWaitFor = 0;

            if (MatchmakerAcceptPatches.IsServer)
            {
                while (!Singleton<FikaServer>.Instantiated && !Singleton<FikaServer>.Instance.ServerReady)
                {
                    await Task.Delay(100);
                }

                FikaServer server = Singleton<FikaServer>.Instance;

                do
                {
                    numbersOfPlayersToWaitFor = MatchmakerAcceptPatches.HostExpectedNumberOfPlayers - (server.NetServer.ConnectedPeersCount + 1);
                    if (MatchmakerAcceptPatches.GClass3163 != null)
                    {
                        if (numbersOfPlayersToWaitFor > 0)
                        {
                            MatchmakerAcceptPatches.GClass3163.ChangeStatus($"Waiting for {numbersOfPlayersToWaitFor} {(numbersOfPlayersToWaitFor > 1 ? "players" : "player")}");
                        }
                        else
                        {
                            MatchmakerAcceptPatches.GClass3163.ChangeStatus($"All players joined, starting game...");
                        }
                    }
                    else
                    {
                        Logger.LogError("WaitForPlayers::GClass3163 was null!");
                    }
                    await Task.Delay(1000);
                } while (numbersOfPlayersToWaitFor > 0 && !forceStart);
            }
            else if (MatchmakerAcceptPatches.IsClient)
            {
                while (!Singleton<FikaClient>.Instantiated && !Singleton<FikaClient>.Instance.ClientReady)
                {
                    await Task.Delay(100);
                }

                FikaClient client = Singleton<FikaClient>.Instance;

                while (client.NetClient == null)
                {
                    await Task.Delay(500);
                }

                int connectionAttempts = 0;

                while (client.ServerConnection == null && connectionAttempts < 5)
                {
                    // Server retries 10 times with a 500ms interval, we give it 5 seconds to try
                    MatchmakerAcceptPatches.GClass3163.ChangeStatus("Waiting for client to connect to server... If there is no notification it failed.");
                    connectionAttempts++;
                    await Task.Delay(1000);

                    if (client.ServerConnection == null && connectionAttempts == 5)
                    {
                        Singleton<PreloaderUI>.Instance.ShowErrorScreen("Network Error", "Unable to connect to the raid server. Make sure ports are forwarded and/or UPnP is enabled and supported.");
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
                    numbersOfPlayersToWaitFor = MatchmakerAcceptPatches.HostExpectedNumberOfPlayers - (client.ConnectedClients + 1);
                    if (MatchmakerAcceptPatches.GClass3163 != null)
                    {
                        if (numbersOfPlayersToWaitFor > 0)
                        {
                            MatchmakerAcceptPatches.GClass3163.ChangeStatus($"Waiting for {numbersOfPlayersToWaitFor} {(numbersOfPlayersToWaitFor > 1 ? "players" : "player")}");
                        }
                        else
                        {
                            MatchmakerAcceptPatches.GClass3163.ChangeStatus($"All players joined, starting game...");
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
                } while (numbersOfPlayersToWaitFor > 0 && !forceStart);
            }
        }

        private async Task SetStatus(LocalPlayer myPlayer, LobbyEntry.ELobbyStatus status)
        {
            SetStatusModel statusBody = new(myPlayer.ProfileId, status);
            await FikaRequestHandler.UpdateSetStatus(statusBody);
            Logger.LogInfo("Setting game status to: " + status.ToString());
        }

        /// <summary>
        /// Bot System Starter -> Countdown
        /// </summary>
        /// <param name="startDelay"></param>
        /// <param name="controllerSettings"></param>
        /// <param name="spawnSystem"></param>
        /// <param name="runCallback"></param>
        /// <returns></returns>
        public override IEnumerator vmethod_4(float startDelay, BotControllerSettings controllerSettings, ISpawnSystem spawnSystem, Callback runCallback)
        {
            if (MatchmakerAcceptPatches.IsClient)
            {
                controllerSettings.BotAmount = EBotAmount.NoBots;
            }

            if (MatchmakerAcceptPatches.IsServer)
            {
                BotsPresets profileCreator = new(BackEndSession, wavesSpawnScenario_0.SpawnWaves, GClass579.BossSpawnWaves, nonWavesSpawnScenario_0.GClass1467_0, false);

                GClass813 botCreator = new(this, profileCreator, CreateBot);
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

                if (MatchmakerAcceptPatches.IsClient)
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
            else if (MatchmakerAcceptPatches.IsClient)
            {
                BotsPresets profileCreator = new(BackEndSession, [], [], [], false);

                GClass813 botCreator = new(this, profileCreator, CreateBot);
                BotZone[] botZones = LocationScene.GetAllObjects<BotZone>(false).ToArray();

                // Setting this to an empty array stops the client from downloading bots
                typeof(BotsController).GetField("_allTypes", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(botsController_0, new WildSpawnType[0]);

                botsController_0.Init(this, botCreator, botZones, spawnSystem, wavesSpawnScenario_0.BotLocationModifier,
                    false, false, true, false, false, Singleton<GameWorld>.Instance, Location_0.OpenZones);

                botsController_0.SetSettings(0, [], []);

                Logger.LogInfo($"Location: {Location_0.Name}");
            }

            BackendConfigSettingsClass instance = Singleton<BackendConfigSettingsClass>.Instance;

            bool isWinter = BackEndSession.IsWinter;
            Class420 winterEventController = new();
            Singleton<GameWorld>.Instance.Class420_0 = winterEventController;
            winterEventController.Run(isWinter).HandleExceptions();

            if (startDelay < 5)
            {
                startDelay = 5;
                NotificationManagerClass.DisplayWarningNotification("You have set the deploy timer too low, resetting to 5!");
            }

            DeployScreen(startDelay);

            if (MatchmakerAcceptPatches.IsServer)
            {
                while (Singleton<FikaServer>.Instance.ReadyClients < MatchmakerAcceptPatches.HostExpectedNumberOfPlayers && !forceStart)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            else if (MatchmakerAcceptPatches.IsClient)
            {
                while (Singleton<FikaClient>.Instance.ReadyClients < MatchmakerAcceptPatches.HostExpectedNumberOfPlayers && !forceStart)
                {
                    yield return new WaitForEndOfFrame();
                }
            }

            yield return new WaitForSeconds(startDelay);

            if (MatchmakerAcceptPatches.IsServer)
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

            yield return new WaitForEndOfFrame();

            CreateExfiltrationPointAndInitDeathHandler();

            // Add FreeCamController to GameWorld GameObject
            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<FreeCameraController>();
            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<FikaAirdropsManager>();
            FikaAirdropsManager.ContainerCount = 0;

            SetupBorderzones();

            if (Singleton<GameWorld>.Instance.MineManager != null)
            {
                Singleton<GameWorld>.Instance.MineManager.OnExplosion += OnMineExplode;
            }

            runCallback.Succeed();

            yield break;
        }

        private void DynamicAIRate_SettingChanged(object sender, EventArgs e)
        {
            if (DynamicAI != null)
            {
                DynamicAI.RateChanged(FikaPlugin.DynamicAIRate.Value);
            }
        }

        private void DynamicAI_SettingChanged(object sender, EventArgs e)
        {
            if (DynamicAI != null)
            {
                DynamicAI.EnabledChange(FikaPlugin.DynamicAI.Value);
            }
        }

        private void SetupBorderzones()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            gameWorld.BorderZones = LocationScene.GetAllObjects<BorderZone>(false).ToArray();
            for (int i = 0; i < gameWorld.BorderZones.Length; i++)
            {
                gameWorld.BorderZones[i].Id = i;
            }

            if (MatchmakerAcceptPatches.IsServer)
            {
                foreach (BorderZone borderZone in gameWorld.BorderZones)
                {
                    borderZone.PlayerShotEvent += OnBorderZoneShot;
                }
            }
            else if (MatchmakerAcceptPatches.IsClient)
            {
                foreach (BorderZone borderZone in gameWorld.BorderZones)
                {
                    borderZone.RemoveAuthority();
                }
            }
        }

        private void OnBorderZoneShot(GInterface94 player, BorderZone zone, float arg3, bool arg4)
        {
            BorderZonePacket packet = new()
            {
                ProfileId = player.iPlayer.ProfileId,
                ZoneId = zone.Id
            };
            Singleton<FikaServer>.Instance.SendDataToAll(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

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
            if (MatchmakerAcceptPatches.IsClient)
            {
                Singleton<FikaClient>.Instance.SendData(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else if (MatchmakerAcceptPatches.IsServer)
            {
                Singleton<FikaServer>.Instance.SendDataToAll(new NetDataWriter(), ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
        }

        public void CreateExfiltrationPointAndInitDeathHandler()
        {
            Logger.LogInfo("CreateExfiltrationPointAndInitDeathHandler");

            GameTimer.Start();
            gparam_0.vmethod_0();
            gparam_0.Player.HealthController.DiedEvent += HealthController_DiedEvent;

            InfiltrationPoint = spawnPoint.Infiltration;
            Profile_0.Info.EntryPoint = InfiltrationPoint;
            Logger.LogDebug("SpawnPoint: " + spawnPoint.Id + ", InfiltrationPoint: " + InfiltrationPoint);

            if (MatchmakerAcceptPatches.IsClient)
            {
                CarExtraction carExtraction = FindObjectOfType<CarExtraction>();
                if (carExtraction != null)
                {
                    carExtraction.Subscribee.OnStatusChanged -= carExtraction.OnStatusChangedHandler;
                }
            }

            ExfiltrationControllerClass.Instance.InitAllExfiltrationPoints(Location_0.exits, MatchmakerAcceptPatches.IsClient, "");
            ExfiltrationPoint[] exfilPoints = ExfiltrationControllerClass.Instance.EligiblePoints(Profile_0);

            GameUi.TimerPanel.SetTime(GClass1296.UtcNow, Profile_0.Info.Side, GameTimer.SessionSeconds(), exfilPoints);

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
                            if (MatchmakerAcceptPatches.IsServer)
                            {
                                coopHandler.serverBTR = gameWorld.gameObject.AddComponent<FikaBTRManager_Host>();
                            }
                            else if (MatchmakerAcceptPatches.IsClient)
                            {
                                coopHandler.clientBTR = gameWorld.gameObject.AddComponent<FikaBTRManager_Client>();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Logger.LogError("CreateExfiltrationPointAndInitDeathHandler: Exception thrown during BTR init, check logs.");
                }
            }

            dateTime_0 = GClass1296.Now;
            Status = GameStatus.Started;
            ConsoleScreen.ApplyStartCommands();
        }

        public void UpdateExfilPointFromServer(ExfiltrationPoint point, bool enable)
        {
            exfilManager.UpdateExfilPointFromServer(point, enable);
        }

        public void ResetExfilPointsFromServer(ExfiltrationPoint[] points)
        {
            Dictionary<string, ExitTimerPanel> currentExfils = Traverse.Create(GameUi.TimerPanel).Field("dictionary_0").GetValue<Dictionary<string, ExitTimerPanel>>();
            foreach (ExitTimerPanel exitTimerPanel in currentExfils.Values)
            {
                exitTimerPanel.Close();
            }
            currentExfils.Clear();

            GameUi.TimerPanel.SetTime(GClass1296.UtcNow, Profile_0.Info.Side, GameTimer.SessionSeconds(), points);
        }

        public List<int> ExtractedPlayers { get; } = [];

        /// <summary>
        /// When the local player successfully extracts, enable freecam, notify other players about the extract
        /// </summary>
        /// <param name="player">The local player to start the Coroutine on</param>
        /// <returns></returns>
        public void Extract(CoopPlayer player, ExfiltrationPoint point)
        {
            PreloaderUI preloaderUI = Singleton<PreloaderUI>.Instance;

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
                if (MatchmakerAcceptPatches.IsClient)
                {
                    Singleton<FikaClient>.Instance.SendData(new NetDataWriter(), ref genericPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                }
                else if (MatchmakerAcceptPatches.IsServer)
                {
                    Singleton<FikaServer>.Instance.SendDataToAll(new NetDataWriter(), ref genericPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    ClearHostAI(player);
                }
            }
            catch
            {

            }

            CoopHandler coopHandler = CoopHandler.GetCoopHandler();

            CoopPlayer coopPlayer = (CoopPlayer)player;
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
            GClass3107.Instance.CloseAllScreensForced();

            // Detroys session timer
            EndByTimerScenario endByTimerScenario = GetComponent<EndByTimerScenario>();
            if (endByTimerScenario != null)
            {
                Destroy(endByTimerScenario);
            }
            if (GameUi.TimerPanel.enabled)
            {
                GameUi.TimerPanel.Close();
            }

            player.ActiveHealthController.DiedEvent -= MainPlayerDied;

            if (FikaPlugin.AutoExtract.Value)
            {
                if (MatchmakerAcceptPatches.IsClient)
                {
                    Stop(coopHandler.MyPlayer.ProfileId, MyExitStatus, coopHandler.MyPlayer.ActiveHealthController.IsAlive ? MyExitLocation : null, 0);
                }
                else if (MatchmakerAcceptPatches.IsServer && Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount == 0)
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
                    foreach (KeyValuePair<BotZone, GClass492> kvp in botsController_0.Groups())
                    {
                        foreach (BotsGroup botsGroup in kvp.Value.GetGroups(true))
                        {
                            botsGroup.RemoveEnemy(player);
                            botsGroup.AddAlly(player);
                        }
                    }

                    BotsClass bots = Traverse.Create(botsController_0.BotSpawner).Field("_bots").GetValue<BotsClass>();
                    HashSet<BotOwner> allBots = Traverse.Create(bots).Field("hashSet_0").GetValue<HashSet<BotOwner>>();

                    foreach (BotOwner bot in allBots)
                    {
                        bot.Memory.DeleteInfoAboutEnemy(player);
                    }
                }
            }
        }

        private void HealthController_DiedEvent(EDamageType obj)
        {
            gparam_0.Player.HealthController.DiedEvent -= method_15;
            gparam_0.Player.HealthController.DiedEvent -= HealthController_DiedEvent;

            PlayerOwner.vmethod_1();
            MyExitStatus = ExitStatus.Killed;
            MyExitLocation = null;

            if (FikaPlugin.Instance.ForceSaveOnDeath)
            {
                SavePlayer((CoopPlayer)gparam_0.Player, MyExitStatus, null, true);
            }
        }

        public override void Stop(string profileId, ExitStatus exitStatus, string exitName, float delay = 0f)
        {
            Logger.LogInfo("CoopGame::Stop");

            CoopPlayer myPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            myPlayer.PacketSender.DestroyThis();

            if (myPlayer.Side != EPlayerSide.Savage)
            {
                if (myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem != null)
                {
                    GStruct414<GClass2785> result = InteractionsHandlerClass.Remove(myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem, myPlayer.GClass2761_0, false, true);
                    if (result.Error != null)
                    {
                        FikaPlugin.Instance.FikaLogger.LogWarning("CoopGame::Stop: Error removing dog tag!");
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

            if (MatchmakerAcceptPatches.IsServer)
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
                FikaPlugin.Instance.FikaLogger.LogError("Unable to send RaidLeave request to server.");
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
                Logger.LogError("CoopGame::Stop: Could not find CoopHandler!");
            }

            coopHandler.RunAsyncTasks = false;
            Destroy(coopHandler);

            if (CoopHandler.CoopHandlerParent != null)
            {
                Destroy(CoopHandler.CoopHandlerParent);
            }

            ExitManager stopManager = new(this, exitStatus, exitName, delay, myPlayer);

            EndByExitTrigerScenario endByExitTrigger = GetComponent<EndByExitTrigerScenario>();
            EndByTimerScenario endByTimerScenario = GetComponent<EndByTimerScenario>();
            GameUI gameUI = GameUI.Instance;

            if (endByTimerScenario != null)
            {
                if (Status == GameStatus.Starting || Status == GameStatus.Started)
                {
                    endByTimerScenario.GameStatus_0 = GameStatus.SoftStopping;
                }
            }

            exfilManager.Stop();

            Status = GameStatus.Stopping;
            GameTimer.TryStop();
            endByExitTrigger.Stop();
            if (gameUI.TimerPanel.enabled)
            {
                gameUI.TimerPanel.Close();
            }
            if (EnvironmentManager.Instance != null)
            {
                EnvironmentManager.Instance.Stop();
            }
            MonoBehaviourSingleton<PreloaderUI>.Instance.StartBlackScreenShow(1f, 1f, new Action(stopManager.HandleExit));
            GClass549.Config.UseSpiritPlayer = false;
        }

        private void SavePlayer(CoopPlayer player, ExitStatus exitStatus, string exitName, bool fromDeath)
        {
            if (hasSaved)
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

            //Method taken directly from AKI, can be found in the aki-singleplayer assembly as OfflineSaveProfilePatch
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
        }

        private void StopFromError(string profileId, ExitStatus exitStatus)
        {
            Logger.LogInfo("CoopGame::StopFromError");

            CoopPlayer myPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            myPlayer.PacketSender.DestroyThis();

            if (myPlayer.Side != EPlayerSide.Savage)
            {
                if (myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem != null)
                {
                    GStruct414<GClass2785> result = InteractionsHandlerClass.Remove(myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem, myPlayer.GClass2761_0, false, true);
                    if (result.Error != null)
                    {
                        FikaPlugin.Instance.FikaLogger.LogWarning("CoopGame::StopFromError: Error removing dog tag!");
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
                Logger.LogError("CoopGame::Stop: Could not find CoopHandler!");
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

            ErrorExitManager stopManager = new()
            {
                baseLocalGame_0 = this,
                exitStatus = exitStatus,
                exitName = exitName,
                delay = delay
            };

            EndByExitTrigerScenario endByExitTrigger = GetComponent<EndByExitTrigerScenario>();
            EndByTimerScenario endByTimerScenario = GetComponent<EndByTimerScenario>();
            GameUI gameUI = GameUI.Instance;

            if (endByTimerScenario != null)
            {
                if (Status == GameStatus.Starting || Status == GameStatus.Started)
                {
                    endByTimerScenario.GameStatus_0 = GameStatus.SoftStopping;
                }
            }

            if (exfilManager != null)
            {
                exfilManager.Stop();
            }

            Status = GameStatus.Stopping;
            if (GameTimer != null)
            {
                GameTimer.TryStop();
            }
            if (endByExitTrigger != null)
            {
                endByExitTrigger.Stop();
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
            GClass549.Config.UseSpiritPlayer = false;
        }

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
        }

        public override void Dispose()
        {
            Logger.LogDebug("CoopGame::Dispose()");

            if (Singleton<GameWorld>.Instance.MineManager != null)
            {
                Singleton<GameWorld>.Instance.MineManager.OnExplosion -= OnMineExplode;
            }

            if (extractRoutine != null)
            {
                StopCoroutine(extractRoutine);
            }

            if (MatchmakerAcceptPatches.IsServer)
            {
                CoopPlayer coopPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
                coopPlayer.PacketSender.DestroyThis();

                Singleton<FikaServer>.Instance.NetServer.Stop();
                Singleton<FikaServer>.TryRelease(Singleton<FikaServer>.Instance);

                FikaDynamicAI newDynamicAI = gameObject.GetComponent<FikaDynamicAI>();
                if (newDynamicAI != null)
                {
                    Destroy(newDynamicAI);
                }

                FikaPlugin.DynamicAI.SettingChanged -= DynamicAI_SettingChanged;
                FikaPlugin.DynamicAIRate.SettingChanged -= DynamicAIRate_SettingChanged;
            }
            else if (MatchmakerAcceptPatches.IsClient)
            {
                Singleton<FikaClient>.Instance.NetClient.Stop();
                Singleton<FikaClient>.TryRelease(Singleton<FikaClient>.Instance);

                // Resetting this array to null forces the game to re-allocate it if the client hosts the next session
                typeof(BotsController).GetField("_allTypes", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(botsController_0, null);
            }

            MatchmakerAcceptPatches.Nodes = null;
            MatchmakerAcceptPatches.HostExpectedNumberOfPlayers = 1;

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
                GClass3107 screenManager = GClass3107.Instance;
                if (screenManager.CheckCurrentScreen(EEftScreenType.Reconnect))
                {
                    screenManager.CloseAllScreensForced();
                }
                localGame.gparam_0.Player.OnGameSessionEnd(exitStatus, localGame.PastTime, localGame.Location_0.Id, exitName);
                localGame.CleanUp();
                localGame.Status = GameStatus.Stopped;
                TimeSpan timeSpan = GClass1296.Now - localGame.dateTime_0;
                localGame.ginterface145_0.OfflineRaidEnded(exitStatus, exitName, timeSpan.TotalSeconds).HandleExceptions();
                MonoBehaviourSingleton<BetterAudio>.Instance.FadeOutVolumeAfterRaid();
                StaticManager staticManager = StaticManager.Instance;
                float num = delay;
                Action action;
                if ((action = EndAction) == null)
                {
                    action = (EndAction = new Action(FireCallback));
                }
                staticManager.WaitSeconds(num, action);
            }

            private void FireCallback()
            {
                Callback<ExitStatus, TimeSpan, MetricsClass> endCallback = Traverse.Create(localGame).Field("callback_0").GetValue<Callback<ExitStatus, TimeSpan, MetricsClass>>();

                localGame.SavePlayer(localPlayer, exitStatus, exitName, false);

                endCallback(new Result<ExitStatus, TimeSpan, MetricsClass>(exitStatus, GClass1296.Now - localGame.dateTime_0, new MetricsClass()));
                UIEventSystem.Instance.Enable();
            }
        }

        private class ErrorExitManager : Class1364
        {
            public void ExitOverride()
            {
                GClass3107 instance = GClass3107.Instance;
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
                Action action;
                if ((action = action_0) == null)
                {
                    action = action_0 = new Action(method_1);
                }
                instance2.WaitSeconds(num, action);
            }
        }

        public new void method_6(string backendUrl, string locationId, int variantId)
        {
            Logger.LogDebug("CoopGame::method_6");
            return;
        }
    }
}
