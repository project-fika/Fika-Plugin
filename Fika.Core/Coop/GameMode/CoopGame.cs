using BepInEx.Logging;
using Comfort.Common;
using CommonAssets.Scripts.Game;
using ComponentAce.Compression.Libs.zlib;
using EFT;
using EFT.AssetsManager;
using EFT.Bots;
using EFT.CameraControl;
using EFT.Counters;
using EFT.EnvironmentEffect;
using EFT.Game.Spawning;
using EFT.GameTriggers;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.MovingPlatforms;
using EFT.UI;
using EFT.UI.Matchmaker;
using EFT.UI.Screens;
using EFT.Weather;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.FreeCamera;
using Fika.Core.Coop.HostClasses;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.PacketHandlers;
using Fika.Core.Coop.Patches;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Packets.GameWorld;
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
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Coop.GameMode
{
	/// <summary>
	/// Coop game used in Fika
	/// </summary>
	public sealed class CoopGame : BaseLocalGame<EftGamePlayerOwner>, IBotGame, IFikaGame
	{
		public string InfiltrationPoint;
		public ExitStatus ExitStatus { get; set; } = ExitStatus.Survived;
		public string ExitLocation { get; set; } = null;
		public ISpawnSystem SpawnSystem;
		public Dictionary<string, Player> Bots = [];
		public List<int> ExtractedPlayers { get; } = [];
		public string SpawnId;
		public bool InteractablesInitialized { get; set; } = false;
		public bool HasReceivedLoot { get; set; } = false;
		public List<ThrowWeapItemClass> ThrownGrenades;
		public bool WeatherReady;

		private readonly Dictionary<int, int> botQueue = [];
		private Coroutine extractRoutine;
		private SpawnPointManagerClass spawnPoints = null;
		private ISpawnPoint spawnPoint = null;
		private WavesSpawnScenario wavesSpawnScenario_0;
		private NonWavesSpawnScenario nonWavesSpawnScenario_0;
		private BossSpawnScenario bossSpawnScenario;
		private Func<LocalPlayer, EftGamePlayerOwner> func_1;
		private bool hasSaved = false;
		private CoopExfilManager exfilManager;
		private CoopTimeManager timeManager;
		private CoopHalloweenEventManager halloweenEventManager;
		private FikaDebug fikaDebug;
		private bool isServer;
		private List<string> localTriggerZones = [];
		private DateTime? gameTime;
		private TimeSpan? sessionTime;
		private BotStateManager botStateManager;
		private ESeason season;

		public FikaDynamicAI DynamicAI { get; private set; }
		public RaidSettings RaidSettings { get; private set; }
		public byte[] HostLootItems { get; private set; }
		public GClass1315 LootItems { get; internal set; } = [];
		BossSpawnScenario IBotGame.BossSpawnScenario
		{
			get
			{
				return bossSpawnScenario;
			}
		}
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
		public WeatherClass[] WeatherClasses { get; set; }

		IWeatherCurve IBotGame.WeatherCurve
		{
			get
			{
				return WeatherController.Instance.WeatherCurve;
			}
		}

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
			}
		}

		public SeasonsSettingsClass SeasonsSettings { get; set; }

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
		internal static CoopGame Create(IInputTree inputTree, Profile profile, GameWorld gameWorld, GameDateTime backendDateTime,
			InsuranceCompanyClass insurance, MenuUI menuUI, GameUI gameUI, LocationSettingsClass.Location location,
			TimeAndWeatherSettings timeAndWeather, WavesSettings wavesSettings, EDateTime dateTime,
			Callback<ExitStatus, TimeSpan, MetricsClass> callback, float fixedDeltaTime, EUpdateQueue updateQueue,
			ISession backEndSession, TimeSpan sessionTime, MetricsEventsClass metricsEvents,
			GClass2385 metricsCollector, LocalRaidSettings localRaidSettings, RaidSettings raidSettings)
		{
			Logger = BepInEx.Logging.Logger.CreateLogSource("CoopGame");

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

			CoopGame coopGame = smethod_0<CoopGame>(inputTree, profile, gameWorld, gameTime, insurance, menuUI, gameUI,
				location, timeAndWeather, wavesSettings, dateTime, callback, fixedDeltaTime, updateQueue, backEndSession,
				new TimeSpan?(sessionTime), metricsEvents, metricsCollector, localRaidSettings);
			coopGame.isServer = FikaBackendUtils.IsServer;

			if (timeAndWeather.TimeFlowType != ETimeFlowType.x1)
			{
				float newFlow = timeAndWeather.TimeFlowType.ToTimeFlow();
				coopGame.GameWorld_0.GameDateTime.TimeFactor = newFlow;
				Logger.LogInfo($"Using custom time flow: {newFlow}");
			}

			if (coopGame.isServer)
			{
				// Non Waves Scenario setup
				coopGame.nonWavesSpawnScenario_0 = NonWavesSpawnScenario.smethod_0(coopGame, location, coopGame.botsController_0);
				coopGame.nonWavesSpawnScenario_0.ImplementWaveSettings(wavesSettings);

				// Waves Scenario setup
				WildSpawnWave[] waves = LocalGame.smethod_7(wavesSettings, location.waves);
				coopGame.wavesSpawnScenario_0 = WavesSpawnScenario.smethod_0(coopGame.gameObject, waves, coopGame.botsController_0.ActivateBotsByWave, location);

				// Boss Scenario setup
				BossLocationSpawn[] bossSpawns = [];
				if (wavesSettings.IsBosses)
				{
					bossSpawns = LocalGame.smethod_8(true, wavesSettings, location.BossLocationSpawn);
				}
				coopGame.bossSpawnScenario = BossSpawnScenario.smethod_0(bossSpawns, coopGame.botsController_0.ActivateBotsByWave);

				coopGame.botStateManager = BotStateManager.Create(coopGame, Singleton<FikaServer>.Instance);
			}

			if (OfflineRaidSettingsMenuPatch_Override.UseCustomWeather && coopGame.isServer)
			{
				Logger.LogInfo("Custom weather enabled, initializing curves");
				coopGame.SetupCustomWeather(timeAndWeather);
			}

			SetupGamePlayerOwnerHandler setupGamePlayerOwnerHandler = new(inputTree, insurance, backEndSession, gameUI, coopGame, location);
			coopGame.func_1 = setupGamePlayerOwnerHandler.HandleSetup;

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

			coopGame.ThrownGrenades = [];

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

		public override void vmethod_0()
		{
			gclass689_0 = new(LoggerMode.None, dictionary_0, Bots);
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

		public override void SetMatchmakerStatus(string status, float? progress = null)
		{
			InvokeMatchingStatusChanged(status, progress);
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
			CoopBot coopBot;
			if (Bots.ContainsKey(profile.Id))
			{
				return null;
			}

			profile.SetSpawnedInSession(profile.Info.Side == EPlayerSide.Savage);

			FikaServer server = Singleton<FikaServer>.Instance;
			netId = server.PopNetId();

			MongoID mongoId = MongoID.Generate(true);
			ushort nextOperationId = 0;
			SendCharacterPacket packet = new(new()
			{
				Profile = profile,
				ControllerId = mongoId,
				FirstOperationId = nextOperationId,
				IsZombie = profile.Info.Settings.UseSimpleAnimator
			}, true, true, position, netId);
			packet.PlayerInfoPacket.HealthByteArray = profile.Health.SerializeHealthInfo();
			Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);

			if (server.NetServer.ConnectedPeersCount > 0)
			{
				await WaitForPlayersToLoadBotProfile(netId);
			}

			// Check for GClass increments on filter
			coopBot = await CoopBot.CreateBot(GameWorld_0, netId, position, Quaternion.identity, "Player",
			   "Bot_", EPointOfView.ThirdPerson, profile, true, UpdateQueue, Player.EUpdateMode.Auto,
			   Player.EUpdateMode.Auto, BackendConfigAbstractClass.Config.CharacterController.BotPlayerMode, FikaGlobals.GetOtherPlayerSensitivity,
				FikaGlobals.GetOtherPlayerSensitivity, GClass1599.Default, mongoId, nextOperationId);

			coopBot.Location = Location_0.Id;
			Bots.Add(coopBot.ProfileId, coopBot);

			if (profile.Info.Side is not EPlayerSide.Savage)
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

			coopBot.NetId = netId;
			if (FikaPlugin.DisableBotMetabolism.Value)
			{
				coopBot.HealthController.DisableMetabolism();
			}
			coopHandler.Players.Add(coopBot.NetId, coopBot);

			if (coopBot.PacketSender is BotPacketSender botSender)
			{
				botSender.AssignManager(botStateManager);
			}

			return coopBot;
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
			FikaServer server = Singleton<FikaServer>.Instance;
			int connectedPeers = server.NetServer.ConnectedPeersCount;

			while (botQueue[netId] < connectedPeers)
			{
				if (start.Subtract(DateTime.Now).TotalSeconds >= 30) // ~30 second failsafe
				{
					Logger.LogWarning("WaitForPlayersToLoadBotProfile: Took too long to receive all packets!");
					botQueue.Remove(netId);
					return;
				}

				await Task.Delay(250);
				connectedPeers = server.NetServer.ConnectedPeersCount;
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
			List<CoopPlayer> humanPlayers = BotExtensions.GetPlayers(coopHandler);

			string botKey = BotExtensions.GetFurthestBot(humanPlayers, Bots, out float furthestDistance);

			if (botKey == string.Empty)
			{
#if DEBUG
				Logger.LogWarning("TryDespawnFurthest: botKey was empty");
#endif
				return false;
			}

			if (furthestDistance > BotExtensions.GetDistanceFromPlayers(position, humanPlayers))
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
		internal void DespawnBot(CoopHandler coopHandler, Player bot)
		{
			BotOwner botOwner = bot.AIData.BotOwner;

			botsController_0.Bots.Remove(botOwner);
			bot.HealthController.DiedEvent -= botOwner.method_6; // Unsubscribe from the event to prevent errors.
			BotDespawn(botOwner);
			if (botOwner != null)
			{
				botOwner.Dispose();
			}

			CoopPlayer coopPlayer = (CoopPlayer)bot;
			coopHandler.Players.Remove(coopPlayer.NetId);
			Bots.Remove(bot.ProfileId);
		}
		#endregion

		/// <summary>
		/// The countdown deploy screen
		/// </summary>
		/// <returns></returns>
		public override IEnumerator vmethod_2()
		{
			int timeBeforeDeployLocal = FikaBackendUtils.IsReconnect ? 3 : Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal;
#if DEBUG
			timeBeforeDeployLocal = 3;
#endif

			if (!isServer)
			{
				SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_HOST_FINISH_INIT.Localized());

				FikaClient client = Singleton<FikaClient>.Instance;
				do
				{
					yield return new WaitForEndOfFrame();
				} while (!client.HostReady);
				LootItems = null;
			}
			else
			{
				FikaServer server = Singleton<FikaServer>.Instance;

				DateTime startTime = EFTDateTimeClass.UtcNow.AddSeconds((double)timeBeforeDeployLocal);
				gameTime = startTime;
				server.GameStartTime = startTime;
				sessionTime = GameTimer.SessionTime;

				InformationPacket packet = new(false)
				{
					NumberOfPlayers = server.NetServer.ConnectedPeersCount,
					ReadyPlayers = server.ReadyClients,
					HostReady = true,
					GameTime = gameTime.Value,
					SessionTime = sessionTime.Value
				};

				server.SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);
				HostLootItems = null;
			}

			CoopPlayer coopPlayer = (CoopPlayer)PlayerOwner.Player;
			coopPlayer.PacketSender.Init();

			DateTime dateTime = EFTDateTimeClass.Now.AddSeconds(timeBeforeDeployLocal);
			new MatchmakerFinalCountdown.FinalCountdownScreenClass(Profile_0, dateTime).ShowScreen(EScreenState.Root);
			MonoBehaviourSingleton<BetterAudio>.Instance.FadeInVolumeBeforeRaid(timeBeforeDeployLocal);
			Singleton<GUISounds>.Instance.StopMenuBackgroundMusicWithDelay(timeBeforeDeployLocal);
			GameUi.gameObject.SetActive(true);
			GameUi.TimerPanel.ProfileId = ProfileId;
			yield return new WaitForSeconds(timeBeforeDeployLocal);
			if (!FikaBackendUtils.IsReconnect)
			{
				NetworkTimeSync.Start();
			}
			SyncTransitControllers();
			FikaEventDispatcher.DispatchEvent(new FikaRaidStartedEvent(FikaBackendUtils.IsServer));
		}

		private void SyncTransitControllers()
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

			string profileId = Profile_0.Id;
			if (transitController.summonedTransits.TryGetValue(profileId, out GClass1639 transitData))
			{
				SyncTransitControllersPacket packet = new()
				{
					ProfileId = profileId,
					RaidId = transitData.raidId,
					Count = transitData.count,
					Maps = transitData.maps
				};

				if (isServer)
				{
					Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
					return;
				}

				Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
				return;
			}

			Logger.LogError("SyncTransitControllers: Could not find TransitData in Summonedtransits!");
		}

		/// <summary>
		/// This task ensures that all players are joined and loaded before continuing
		/// </summary>
		/// <returns></returns>
		private async Task WaitForOtherPlayers()
		{
#if DEBUG
			Logger.LogWarning("Starting " + nameof(WaitForOtherPlayers));
#endif
			if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
			{
				if (isServer && FikaBackendUtils.HostExpectedNumberOfPlayers <= 1)
				{
					if (DynamicAI != null)
					{
						DynamicAI.AddHumans();
					}

					Singleton<FikaServer>.Instance.ReadyClients++;
					return;
				}

				float expectedPlayers = FikaBackendUtils.HostExpectedNumberOfPlayers;
				SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized());
				Logger.LogInfo("Waiting for other players to finish loading...");

				if (isServer)
				{
#if DEBUG
					Logger.LogWarning("Server: Waiting for coopHandler.AmountOfHumans < expected players, expected: " + expectedPlayers);
#endif
					FikaServer server = Singleton<FikaServer>.Instance;
					server.ReadyClients++;
					do
					{
						await Task.Yield();
						SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized(), (float)server.ReadyClients / expectedPlayers);
					} while (coopHandler.AmountOfHumans < expectedPlayers);

					InformationPacket packet = new(false)
					{
						NumberOfPlayers = server.NetServer.ConnectedPeersCount,
						ReadyPlayers = server.ReadyClients
					};

					server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);

#if DEBUG
					Logger.LogWarning("Server: Waiting for server.ReadyClients < expected players, expected: " + expectedPlayers);
#endif
					do
					{
						await Task.Yield();
						SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized(), (float)server.ReadyClients / expectedPlayers);
					} while (server.ReadyClients < expectedPlayers);

					foreach (CoopPlayer player in coopHandler.Players.Values)
					{
						SyncNetIdPacket syncPacket = new(player.ProfileId, player.NetId);
						server.SendDataToAll(ref syncPacket, DeliveryMethod.ReliableUnordered);
					}

					if (DynamicAI != null)
					{
						DynamicAI.AddHumans();
					}

					InformationPacket finalPacket = new(false)
					{
						NumberOfPlayers = server.NetServer.ConnectedPeersCount,
						ReadyPlayers = server.ReadyClients
					};

					server.SendDataToAll(ref finalPacket, DeliveryMethod.ReliableOrdered);
				}
				else
				{
#if DEBUG
					Logger.LogWarning("Client: Waiting for coopHandler.AmountOfHumans < expected players, expected: " + expectedPlayers);
#endif
					FikaClient client = Singleton<FikaClient>.Instance;
					do
					{
						await Task.Yield();
						SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized(), (float)client.ReadyClients / expectedPlayers);
					} while (coopHandler.AmountOfHumans < expectedPlayers);


					InformationPacket packet = new(true)
					{
						ReadyPlayers = 1
					};

					client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
#if DEBUG
					Logger.LogWarning("Client: Waiting for client.ReadyClients < expected players, expected: " + expectedPlayers);
#endif
					do
					{
						await Task.Yield();
						SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized(), (float)client.ReadyClients / expectedPlayers);
					} while (client.ReadyClients < expectedPlayers);
				}
			}
		}

		public string GetSpawnpointName()
		{
			if (!string.IsNullOrEmpty(SpawnId))
			{
				return SpawnId;
			}
			return string.Empty;
		}

		/// <summary>
		/// Sends or receives the <see cref="ISpawnPoint"/> for the game
		/// </summary>
		/// <returns></returns>
		private async Task SendOrReceiveSpawnPoint()
		{
			SetMatchmakerStatus(LocaleUtils.UI_RETRIEVE_SPAWN_INFO.Localized());
			if (isServer)
			{
				bool spawnTogether = RaidSettings.PlayersSpawnPlace == EPlayersSpawnPlace.SamePlace;
				if (spawnTogether)
				{
					Logger.LogInfo($"Setting spawn point to name: '{spawnPoint.Name}', id: '{spawnPoint.Id}'");
					SpawnId = spawnPoint.Id;
				}
				else
				{
					Logger.LogInfo("Using random spawn points!");
					NotificationManagerClass.DisplayMessageNotification(LocaleUtils.RANDOM_SPAWNPOINTS.Localized(), iconType: EFT.Communications.ENotificationIconType.Alert);
					SpawnId = "RANDOM";
				}
			}
			else
			{
				SpawnpointPacket packet = new(true);
				FikaClient client = Singleton<FikaClient>.Instance;

				do
				{
					client.SendData(ref packet, DeliveryMethod.ReliableUnordered);
					await Task.Delay(1000);
					if (string.IsNullOrEmpty(SpawnId))
					{
						await Task.Delay(2000);
					}
				} while (string.IsNullOrEmpty(SpawnId));

				Logger.LogInfo($"Retrieved spawn point id '{SpawnId}' from server");

				if (SpawnId != "RANDOM")
				{
					Dictionary<ISpawnPoint, SpawnPointMarker> allSpawnPoints = Traverse.Create(spawnPoints).Field<Dictionary<ISpawnPoint, SpawnPointMarker>>("dictionary_0").Value;
					foreach (ISpawnPoint spawnPointObject in allSpawnPoints.Keys)
					{
						if (spawnPointObject.Id == SpawnId)
						{
							spawnPoint = spawnPointObject;
						}
					}
				}
				else
				{
					Logger.LogInfo("Spawn Point was random");
					NotificationManagerClass.DisplayMessageNotification(LocaleUtils.RANDOM_SPAWNPOINTS.Localized(), iconType: EFT.Communications.ENotificationIconType.Alert);
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
		public override async Task<LocalPlayer> vmethod_3(GameWorld gameWorld, int playerId, Vector3 position, Quaternion rotation,
			string layerName, string prefix, EPointOfView pointOfView, Profile profile, bool aiControl,
			EUpdateQueue updateQueue, Player.EUpdateMode armsUpdateMode, Player.EUpdateMode bodyUpdateMode,
			CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity,
			Func<float> getAimingSensitivity, IStatisticsManager statisticsManager, ISession session,
			ELocalMode localMode)
		{
			bool spawnedInSession = profile.Side == EPlayerSide.Savage || TransitControllerAbstractClass.IsTransit(profile.Id, out int _);
			profile.SetSpawnedInSession(spawnedInSession);

			statisticsManager = FikaBackendUtils.IsDedicated ? new ObservedStatisticsManager() : new GClass1999();

			CoopPlayer coopPlayer = await CoopPlayer.Create(gameWorld, playerId, spawnPoint.Position, spawnPoint.Rotation, "Player", "Main_", EPointOfView.FirstPerson,
				profile, false, UpdateQueue, armsUpdateMode, Player.EUpdateMode.Auto,
				BackendConfigAbstractClass.Config.CharacterController.ClientPlayerMode, getSensitivity, getAimingSensitivity,
				statisticsManager, new GClass1598(), session, localMode, isServer ? 0 : 1000);

			coopPlayer.Location = Location_0.Id;

			if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
			{
				Logger.LogError($"{nameof(vmethod_3)}:Unable to find {nameof(CoopHandler)}");
				throw new MissingComponentException("CoopHandler was missing during CoopGame init");
			}

			if (RaidSettings.MetabolismDisabled)
			{
				coopPlayer.HealthController.DisableMetabolism();
				NotificationManagerClass.DisplayMessageNotification(LocaleUtils.METABOLISM_DISABLED.Localized(), iconType: EFT.Communications.ENotificationIconType.Alert);
			}

			coopHandler.Players.Add(coopPlayer.NetId, coopPlayer);
			coopHandler.HumanPlayers.Add(coopPlayer);
			coopPlayer.SetupMainPlayer();

			PlayerSpawnRequest body = new(coopPlayer.ProfileId, FikaBackendUtils.GroupId);
			await FikaRequestHandler.UpdatePlayerSpawn(body);

			coopPlayer.SpawnPoint = spawnPoint;

			GameObject customButton = null;

			await NetManagerUtils.SetupGameVariables(isServer, coopPlayer);
			customButton = CreateCancelButton(coopPlayer, customButton);

			if (!isServer && !FikaBackendUtils.IsReconnect)
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
					packet.PlayerInfoPacket.ControllerType = GClass1808.FromController(coopPlayer.HandsController);
					packet.PlayerInfoPacket.ItemId = coopPlayer.HandsController.Item.Id;
					packet.PlayerInfoPacket.IsStationary = coopPlayer.MovementContext.IsStationaryWeaponInHands;
				}

				do
				{
					await Task.Delay(250);
				} while (client.NetClient.FirstPeer == null);

				client.SendData(ref packet, DeliveryMethod.ReliableUnordered);
			}

			fikaDebug = gameObject.AddComponent<FikaDebug>();

			Destroy(customButton);

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
							PlayerLeftRequest playerLeftRequest = new(FikaBackendUtils.Profile.ProfileId);
							FikaRequestHandler.RaidLeave(playerLeftRequest);
						}, null);
				});
				Traverse.Create(backButtonComponent).Field("OnClick").SetValue(newEvent);
			}

			return customButton;
		}

		/// <summary>
		/// Initializes the local player
		/// </summary>
		/// <param name="botsSettings"></param>
		/// <param name="backendUrl"></param>
		/// <param name="runCallback"></param>
		/// <returns></returns>
		public async Task InitPlayer(BotControllerSettings botsSettings, string backendUrl)
		{
			Status = GameStatus.Running;
			UnityEngine.Random.InitState((int)EFTDateTimeClass.Now.Ticks);

			if (isServer)
			{
				await NetManagerUtils.CreateCoopHandler();
			}
			else
			{
				await WaitForHostToLoad();
			}

			if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
			{
				coopHandler.LocalGameInstance = this;
				if (isServer && FikaBackendUtils.IsTransit)
				{
					coopHandler.ReInitInteractables();
				}
			}
			else
			{
				throw new NullReferenceException("CoopHandler was missing!");
			}

			GameWorld gameWorld = Singleton<GameWorld>.Instance;
			gameWorld.LocationId = Location_0.Id;

			ExfiltrationControllerClass.Instance.InitAllExfiltrationPoints(Location_0._Id, Location_0.exits, !isServer, Location_0.DisabledScavExits);

			Logger.LogInfo($"Location: {Location_0.Name}");
			BackendConfigSettingsClass instance = Singleton<BackendConfigSettingsClass>.Instance;

			if (instance != null && instance.ArtilleryShelling != null && instance.ArtilleryShelling.ArtilleryMapsConfigs != null &&
				instance.ArtilleryShelling.ArtilleryMapsConfigs.Keys.Contains(Location_0.Id))
			{
				if (isServer)
				{
					gameWorld.ServerShellingController = new GClass607();
				}
				gameWorld.ClientShellingController = new GClass1381(isServer);
			}

			if (instance != null && instance.EventSettings.EventActive && !instance.EventSettings.LocationsToIgnore.Contains(Location_0.Id))
			{
#if DEBUG
				Logger.LogWarning("Spawning halloween prefabs");
#endif
				gameWorld.HalloweenEventController = new HalloweenEventControllerClass();
				GameObject gameObject = (GameObject)Resources.Load("Prefabs/HALLOWEEN_CONTROLLER");
				if (gameObject != null)
				{
					transform.InstantiatePrefab(gameObject);
				}
				else
				{
					Logger.LogError("CoopGame::vmethod_1: Error loading Halloween assets!");
				}

				if (isServer)
				{
					halloweenEventManager = gameWorld.gameObject.GetOrAddComponent<CoopHalloweenEventManager>();
				}
			}

			if (FikaPlugin.Instance.UseBTR && instance != null && instance.BTRSettings.LocationsWithBTR.Contains(Location_0.Id))
			{
#if DEBUG
				Logger.LogWarning("Spawning BTR controller");
#endif
				gameWorld.BtrController = new BTRControllerClass(gameWorld);
			}

			bool transitActive;
			if (instance == null)
			{
				transitActive = false;
			}
			else
			{
				BackendConfigSettingsClass.GClass1529 transitSettings = instance.transitSettings;
				transitActive = transitSettings != null && transitSettings.active;
			}
			if (transitActive && FikaPlugin.Instance.EnableTransits)
			{
				gameWorld.TransitController = isServer ? new FikaHostTransitController(instance.transitSettings, Location_0.transitParameters,
					Profile_0, localRaidSettings_0) : new FikaClientTransitController(instance.transitSettings, Location_0.transitParameters,
					Profile_0, localRaidSettings_0);
			}
			else
			{
				Logger.LogInfo("Transits are disabled");
				TransitControllerAbstractClass.DisableTransitPoints();
			}

			ApplicationConfigClass config = BackendConfigAbstractClass.Config;
			if (config.FixedFrameRate > 0f)
			{
				FixedDeltaTime = 1f / config.FixedFrameRate;
			}

			if (FikaBackendUtils.IsReconnect)
			{
				await GetReconnectProfile(ProfileId);
			}

			using (CounterCreatorAbstractClass.StartWithToken("player create"))
			{
				LocalPlayer player = await CreateLocalPlayer();
				dictionary_0.Add(player.ProfileId, player);
				gparam_0 = func_1(player);
				PlayerCameraController.Create(gparam_0.Player);
				CameraClass.Instance.SetOcclusionCullingEnabled(Location_0.OcculsionCullingEnabled);
				CameraClass.Instance.IsActive = false;

				if (FikaBackendUtils.IsDedicated && gameWorld.TransitController is FikaHostTransitController hostController)
				{
					hostController.SetupDedicatedPlayerTransitStash(player);
				}
			}

			await WaitForPlayersToConnect();

			LocationSettingsClass.Location location = localRaidSettings_0.selectedLocation;
			if (isServer)
			{
				HostLootItems = SimpleZlib.CompressToBytes(location.Loot.ToJson([]), 6);
				await method_11(location);
			}
			else
			{
				SetMatchmakerStatus(LocaleUtils.UI_RETRIEVE_LOOT.Localized());
				if (!FikaBackendUtils.IsReconnect)
				{
					await RetrieveLootFromServer(true);
				}
				else
				{
					await RetrieveLootFromServer(false);
				}
				location.Loot = LootItems;
				await method_11(location);
			}

			if (FikaBackendUtils.IsReconnect)
			{
				await Reconnect();
				foreach (KeyValuePair<EBodyPart, GClass2747<ActiveHealthController.GClass2746>.BodyPartState> item in gparam_0.Player.ActiveHealthController.Dictionary_0)
				{
					if (item.Value.Health.AtMinimum)
					{
						item.Value.IsDestroyed = true;
					}
				}
			}

			coopHandler.SetReady(true);
			await vmethod_1(botsSettings, SpawnSystem);

			if (isServer && Singleton<IBotGame>.Instantiated)
			{
				Singleton<IBotGame>.Instance.BotsController.CoversData.Patrols.RestoreLoot(Location_0.Loot, LocationScene.GetAllObjects<LootableContainer>(false));
			}

			if (isServer)
			{
				GClass2404 gclass = new()
				{
					AirdropParameters = Location_0.airdropParameters
				};
				gclass.Init(true);
				(Singleton<GameWorld>.Instance as ClientGameWorld).ClientSynchronizableObjectLogicProcessor.ServerAirdropManager = gclass;
				Traverse.Create(GameWorld_0.SynchronizableObjectLogicProcessor).Field<GInterface241>("ginterface241_0").Value = Singleton<FikaServer>.Instance;
			}

			await method_6();
			FikaEventDispatcher.DispatchEvent(new GameWorldStartedEvent(GameWorld_0));
		}

		private async Task WaitForHostToLoad()
		{
			FikaClient client = Singleton<FikaClient>.Instance;

			InformationPacket packet = new(true);
			do
			{
				SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_HOST_INIT.Localized());
				client.SendData(ref packet, DeliveryMethod.ReliableOrdered);

				await Task.Delay(1000);
			} while (!client.HostLoaded);
		}

		private async Task GetReconnectProfile(string profileId)
		{
			Profile_0 = null;

			ReconnectPacket reconnectPacket = new(true)
			{
				InitialRequest = true,
				ProfileId = profileId
			};
			FikaClient client = Singleton<FikaClient>.Instance;
			client.SendData(ref reconnectPacket, DeliveryMethod.ReliableUnordered);

			do
			{
				await Task.Delay(250);
			} while (Profile_0 == null);

			await Singleton<PoolManager>.Instance.LoadBundlesAndCreatePools(PoolManager.PoolsCategory.Raid, PoolManager.AssemblyType.Local,
				Profile_0.GetAllPrefabPaths(true).ToArray(), JobPriority.General);
		}

		private async Task Reconnect()
		{
			SetMatchmakerStatus(LocaleUtils.UI_RECONNECTING.Localized());

			ReconnectPacket reconnectPacket = new(true)
			{
				ProfileId = ProfileId
			};
			FikaClient client = Singleton<FikaClient>.Instance;
			client.SendData(ref reconnectPacket, DeliveryMethod.ReliableUnordered);

			do
			{
				await Task.Delay(1000);
			} while (!client.ReconnectDone);
		}

		private async Task RetrieveLootFromServer(bool register)
		{
			FikaClient client = Singleton<FikaClient>.Instance;
			WorldLootPacket packet = new(true);
			do
			{
				client.SendData(ref packet, DeliveryMethod.ReliableUnordered);
				await Task.Delay(1000);
				if (!HasReceivedLoot && LootItems.Count < 1)
				{
					await Task.Delay(2000);
				}
			} while (!HasReceivedLoot);

			if (register)
			{
				RegisterPlayerRequest request = new(0, Location_0.Id, 0);
				await FikaRequestHandler.RegisterPlayer(request);
			}
		}

		/// <summary>
		/// Creates the local player
		/// </summary>
		/// <returns>A <see cref="Player"/></returns>
		private async Task<LocalPlayer> CreateLocalPlayer()
		{
			int num = 0;

			Player.EUpdateMode eupdateMode = Player.EUpdateMode.Auto;
			if (BackendConfigAbstractClass.Config.UseHandsFastAnimator)
			{
				eupdateMode = Player.EUpdateMode.Manual;
			}

			spawnPoints = SpawnPointManagerClass.CreateFromScene(new DateTime?(EFTDateTimeClass.LocalDateTimeFromUnixTime(Location_0.UnixDateTime)),
				Location_0.SpawnPointParams);
			int spawnSafeDistance = (Location_0.SpawnSafeDistanceMeters > 0) ? Location_0.SpawnSafeDistanceMeters : 100;
			GStruct390 settings = new(Location_0.MinDistToFreePoint, Location_0.MaxDistToFreePoint, Location_0.MaxBotPerZone, spawnSafeDistance);
			SpawnSystem = GClass3304.CreateSpawnSystem(settings, FikaGlobals.GetApplicationTime, Singleton<GameWorld>.Instance, botsController_0, spawnPoints);

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

				await InitInteractables();
				await InitExfils();
			}

			if (Location_0.AccessKeys != null && Location_0.AccessKeys.Length > 0)
			{
				IEnumerable<Item> items = Profile_0.Inventory.GetPlayerItems(EPlayerItems.Equipment);
				if (items != null)
				{
					Class1487 keyFinder = new()
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

			IStatisticsManager statisticsManager = new CoopClientStatisticsManager(Profile_0);

			LocalPlayer myPlayer = await vmethod_3(GameWorld_0, num, spawnPoint.Position, spawnPoint.Rotation, "Player", "", EPointOfView.FirstPerson,
				Profile_0, false, UpdateQueue, eupdateMode, Player.EUpdateMode.Auto,
				BackendConfigAbstractClass.Config.CharacterController.ClientPlayerMode,
				FikaGlobals.GetLocalPlayerSensitivity, FikaGlobals.GetLocalPlayerAimingSensitivity, statisticsManager,
				iSession, (localRaidSettings_0 != null) ? localRaidSettings_0.mode : ELocalMode.TRAINING);

			myPlayer.OnEpInteraction += OnEpInteraction;

			return myPlayer;
		}

		private async Task InitExfils()
		{
			SetMatchmakerStatus(LocaleUtils.UI_RETRIEVE_EXFIL_DATA.Localized());
			FikaClient client = Singleton<FikaClient>.Instance;
			ExfiltrationPacket exfilPacket = new(true);

			do
			{
				client.SendData(ref exfilPacket, DeliveryMethod.ReliableUnordered);
				await Task.Delay(1000);
				if (!client.ExfilPointsReceived)
				{
					await Task.Delay(2000);
				}

			} while (!client.ExfilPointsReceived);
		}

		private async Task InitInteractables()
		{
			SetMatchmakerStatus(LocaleUtils.UI_RETRIEVE_INTERACTABLES.Localized());
			FikaClient client = Singleton<FikaClient>.Instance;
			InteractableInitPacket packet = new(true);

			do
			{
				client.SendData(ref packet, DeliveryMethod.ReliableUnordered);
				await Task.Delay(1000);
				if (!InteractablesInitialized)
				{
					await Task.Delay(2000);
				}
			} while (!InteractablesInitialized);
		}


		/// <summary>
		/// <see cref="Task"/> used to wait for all other players to join the game
		/// </summary>
		/// <returns></returns>
		private async Task WaitForPlayersToConnect()
		{
			Logger.LogInfo("Starting task to wait for other players.");

			SetMatchmakerStatus(LocaleUtils.UI_INIT_COOP_GAME.Localized());
			int numbersOfPlayersToWaitFor = 0;

			string localizedPlayer = LocaleUtils.UI_WAIT_FOR_PLAYER.Localized();
			string localizedPlayers = LocaleUtils.UI_WAIT_FOR_PLAYERS.Localized();

			if (isServer)
			{
				FikaServer server = Singleton<FikaServer>.Instance;
				server.RaidInitialized = true;

				do
				{
					numbersOfPlayersToWaitFor = FikaBackendUtils.HostExpectedNumberOfPlayers - (server.NetServer.ConnectedPeersCount + 1);
					if (numbersOfPlayersToWaitFor > 0)
					{
						bool multiple = numbersOfPlayersToWaitFor > 1;
						SetMatchmakerStatus(string.Format(multiple ? localizedPlayers : localizedPlayer,
							numbersOfPlayersToWaitFor));
					}
					else
					{
						SetMatchmakerStatus(LocaleUtils.UI_ALL_PLAYERS_JOINED.Localized());
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
					SetMatchmakerStatus(LocaleUtils.UI_WAITING_FOR_CONNECT.Localized());
					connectionAttempts++;
					await Task.Delay(1000);

					if (client.ServerConnection == null && connectionAttempts == 5)
					{
						Singleton<PreloaderUI>.Instance.ShowErrorScreen(LocaleUtils.UI_ERROR_CONNECTING.Localized(),
							LocaleUtils.UI_ERROR_CONNECTING_TO_RAID.Localized());
					}
				}

				while (client == null)
				{
					await Task.Delay(500);
				}

				InformationPacket packet = new(true);
				client.SendData(ref packet, DeliveryMethod.ReliableUnordered);
				do
				{
					numbersOfPlayersToWaitFor = FikaBackendUtils.HostExpectedNumberOfPlayers - (client.ConnectedClients + 1);
					if (numbersOfPlayersToWaitFor > 0)
					{
						bool multiple = numbersOfPlayersToWaitFor > 1;
						SetMatchmakerStatus(string.Format(multiple ? localizedPlayers : localizedPlayer,
							numbersOfPlayersToWaitFor));
					}
					else
					{
						SetMatchmakerStatus(LocaleUtils.UI_ALL_PLAYERS_JOINED.Localized());
					}
					client.SendData(ref packet, DeliveryMethod.ReliableUnordered);
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
		public override async Task vmethod_1(BotControllerSettings controllerSettings, ISpawnSystem spawnSystem)
		{
			GameWorld gameWorld = Singleton<GameWorld>.Instance;
			gameWorld.RegisterRestrictableZones();

			if (isServer)
			{
				BotsPresets botsPresets = new(iSession, wavesSpawnScenario_0.SpawnWaves,
					bossSpawnScenario.BossSpawnWaves, nonWavesSpawnScenario_0.GClass1622_0, false);
				List<WaveInfo> waveInfos = [];
				LocationSettingsClass.Location.GClass1336 halloween = Location_0.Events.Halloween2024;
				if (halloween != null && halloween.InfectionPercentage > 0)
				{
					waveInfos.AddRange(BotHalloweenWithZombies.GetProfilesOnStart());
				}
				await botsPresets.TryLoadBotsProfilesOnStart(waveInfos);
				GClass888 botCreator = new(this, botsPresets, CreateBot);
				BotZone[] botZones = LocationScene.GetAllObjects<BotZone>(false).ToArray();

				bool useWaveControl = controllerSettings.BotAmount == EBotAmount.Horde;

				botsController_0.Init(this, botCreator, botZones, spawnSystem, wavesSpawnScenario_0.BotLocationModifier,
					controllerSettings.IsEnabled, controllerSettings.IsScavWars, useWaveControl, false,
					bossSpawnScenario.HaveSectants, gameWorld, Location_0.OpenZones, Location_0.Events);

				int numberOfBots = controllerSettings.BotAmount switch
				{
					EBotAmount.AsOnline => 20,
					EBotAmount.NoBots => 0,
					EBotAmount.Low => 15,
					EBotAmount.Medium => 20,
					EBotAmount.High => 25,
					EBotAmount.Horde => 35,
					_ => 15,
				};

				botsController_0.SetSettings(numberOfBots, iSession.BackEndConfig.BotPresets, iSession.BackEndConfig.BotWeaponScatterings);
				if (!FikaBackendUtils.IsDedicated)
				{
					botsController_0.AddActivePLayer(PlayerOwner.Player);
				}

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

			await WaitForOtherPlayers();

			SetMatchmakerStatus(LocaleUtils.UI_FINISHING_RAID_INIT.Localized());
			Logger.LogInfo("All players are loaded, continuing...");

			if (isServer)
			{
#if DEBUG
				Logger.LogWarning("Server: Starting scenarios of bots");
#endif
				if (Location_0.OldSpawn && wavesSpawnScenario_0.SpawnWaves != null && wavesSpawnScenario_0.SpawnWaves.Length != 0)
				{
					Logger.LogInfo("Running old spawn system. Waves: " + wavesSpawnScenario_0.SpawnWaves.Length);
					if (wavesSpawnScenario_0 != null)
					{
						await wavesSpawnScenario_0.Run(EBotsSpawnMode.BeforeGameStarted);
						await wavesSpawnScenario_0.Run(EBotsSpawnMode.AfterGameStarted);
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

				bossSpawnScenario.Run(EBotsSpawnMode.Anyway);
				botsController_0.EventsController.SpawnAction();

				FikaPlugin.DynamicAI.SettingChanged += DynamicAI_SettingChanged;
				FikaPlugin.DynamicAIRate.SettingChanged += DynamicAIRate_SettingChanged;

				SetStatusModel status = new(FikaBackendUtils.GroupId, LobbyEntry.ELobbyStatus.IN_GAME);

				await FikaRequestHandler.UpdateSetStatus(status);
			}

			// Add FreeCamController to GameWorld GameObject
			FreeCameraController freeCamController = gameWorld.gameObject.AddComponent<FreeCameraController>();
			Singleton<FreeCameraController>.Create(freeCamController);

			await SetupRaidCode();

			// This will be implemented later, suspect it's used for reconnects?
			/*if (isServer && gameWorld.PlatformAdapters.Length > 0)
			{
				MovingPlatform.GClass2952 adapter = gameWorld.PlatformAdapters[0];
				adapter.Platform.TravelState.Bind(HandleHostTrain);
			}*/

			Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal = Math.Max(Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal, 3);
			GameWorld_0.RegisterBorderZones();
		}

		public override IEnumerator vmethod_5(Action runCallback)
		{
			if (WeatherController.Instance != null)
			{
				SetMatchmakerStatus(LocaleUtils.UI_INIT_WEATHER.Localized());
				Logger.LogInfo("Generating and initializing weather...");
				if (isServer)
				{
					Task<GClass1310> weatherTask = iSession.WeatherRequest();
					while (!weatherTask.IsCompleted)
					{
						yield return new WaitForEndOfFrame();
					}
					GClass1310 weather = weatherTask.Result;
					Season = weather.Season;
					SeasonsSettings = weather.SeasonsSettings;
					if (!OfflineRaidSettingsMenuPatch_Override.UseCustomWeather)
					{
						WeatherClasses = weather.Weathers;
						WeatherController.Instance.method_0(WeatherClasses);
					}
				}
				else if (!isServer)
				{
					Task getWeather = GetWeather();
					while (!getWeather.IsCompleted)
					{
						yield return new WaitForEndOfFrame();
					}
					WeatherController.Instance.method_0(WeatherClasses);
				}
			}

			SetMatchmakerStatus(LocaleUtils.UI_FINISHING_RAID_INIT.Localized());

			GameWorld_0.TriggersModule = gameObject.AddComponent<LocalClientTriggersModule>();
			GameWorld_0.FillLampControllers();

			WeatherReady = true;
			OfflineRaidSettingsMenuPatch_Override.UseCustomWeather = false;

			Class442 seasonController = new();
			GameWorld_0.GInterface29_0 = seasonController;

#if DEBUG
			Logger.LogWarning("Running season handler");
#endif
			Task runSeason = seasonController.Run(Season, SeasonsSettings);
			while (!runSeason.IsCompleted)
			{
				yield return new WaitForEndOfFrame();
			}
			yield return base.vmethod_5(runCallback);
		}

		private async Task GetWeather()
		{
			WeatherPacket packet = new()
			{
				IsRequest = true,
				HasData = false
			};

			FikaClient client = Singleton<FikaClient>.Instance;
			client.SendData(ref packet, DeliveryMethod.ReliableUnordered);

			while (WeatherClasses == null)
			{
				await Task.Delay(1000);
				client.SendData(ref packet, DeliveryMethod.ReliableUnordered);
			}
		}

		/// <summary>
		/// Currently unused
		/// </summary>
		/// <param name="state"></param>
		[Obsolete("Not implemented yet", true)]
		private void HandleHostTrain(Locomotive.ETravelState state)
		{
			MovingPlatform.GClass3310 platformAdapter = Singleton<GameWorld>.Instance.PlatformAdapters[0];
			if (!platformAdapter.HasNetPacket)
			{
				return;
			}

			Locomotive platform = Singleton<GameWorld>.Instance.PlatformAdapters[0].Platform;
			FikaServer server = Singleton<FikaServer>.Instance;

			/*GenericPacket genericPacket = new()
			{
				PacketType = EPackageType.TrainSync,
				PlatformId = platformAdapter.Id,
				PlatformPosition = platform.NormalCurvePosition
			};

			server.SendDataToAll(ref genericPacket, DeliveryMethod.ReliableUnordered);*/
		}

		private Task SetupRaidCode()
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
			GameTimer.Start(gameTime, sessionTime);
			Spawn();

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

			ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;
			Player player = gparam_0.Player;
			bool isScav = player.Side is EPlayerSide.Savage;
			ExfiltrationPoint[] exfilPoints;

			if (isScav)
			{
				exfilController.ScavExfiltrationClaim(player.Position, player.ProfileId, player.Profile.FenceInfo.AvailableExitsCount);
				int mask = exfilController.GetScavExfiltrationMask(player.ProfileId);
				exfilPoints = exfilController.ScavExfiltrationClaim(mask, player.ProfileId);
			}
			else
			{
				exfilPoints = exfilController.EligiblePoints(Profile_0);
			}

			GameUi.TimerPanel.SetTime(EFTDateTimeClass.UtcNow, Profile_0.Info.Side, GameTimer.EscapeTimeSeconds(), exfilPoints);

			if (isServer)
			{
				if (TransitControllerAbstractClass.Exist(out FikaHostTransitController gclass))
				{
					gclass.Init();
					// TODO: Sync to clients!!!
				}
			}
			else
			{
				if (TransitControllerAbstractClass.Exist(out FikaClientTransitController gclass))
				{
					gclass.Init();
				}
			}

			exfilManager = gameObject.AddComponent<CoopExfilManager>();
			exfilManager.Run(exfilPoints);

			dateTime_0 = EFTDateTimeClass.Now;
			Status = GameStatus.Started;

			if (isServer)
			{
				BotsController.Bots.CheckActivation();
			}

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
		/// When the local player successfully extracts, enable freecam, notify other players about the extract
		/// </summary>
		/// <param name="player">The local player to start the Coroutine on</param>
		/// <param name="exfiltrationPoint">The exfiltration point that was used to extract</param>
		/// <param name="transitPoint">The transit point that was used to transit</param>
		/// <returns></returns>
		public void Extract(CoopPlayer player, ExfiltrationPoint exfiltrationPoint, TransitPoint transitPoint = null)
		{
			PreloaderUI preloaderUI = Singleton<PreloaderUI>.Instance;
			localTriggerZones = new(player.TriggerZones);

			player.ClientMovementContext.SetGravity(false);
			Vector3 position = player.Position;
			position.y += 500;
			player.Teleport(position);

			if (ExitStatus == ExitStatus.MissingInAction)
			{
				NotificationManagerClass.DisplayMessageNotification(LocaleUtils.PLAYER_MIA.Localized(), iconType: EFT.Communications.ENotificationIconType.Alert, textColor: Color.red);
			}

			if (player.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
			{
				sharedQuestController.ToggleQuestSharing(false);
			}

			BackendConfigSettingsClass.GClass1503.GClass1509 matchEndConfig = Singleton<BackendConfigSettingsClass>.Instance.Experience.MatchEnd;
			if (player.Profile.EftStats.SessionCounters.GetAllInt([CounterTag.Exp]) < matchEndConfig.SurvivedExpRequirement && PastTime < matchEndConfig.SurvivedTimeRequirement)
			{
				ExitStatus = ExitStatus.Runner;
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
			if (transitController != null)
			{
				if (transitController.alreadyTransits.TryGetValue(player.ProfileId, out GClass1926 data))
				{
					ExitStatus = ExitStatus.Transit;
					ExitLocation = transitPoint.parameters.name;
					FikaBackendUtils.IsTransit = true;
				}
				if (transitController is FikaHostTransitController hostController)
				{
					GenericPacket backendPacket = new()
					{
						NetId = player.NetId,
						Type = EGenericSubPacketType.UpdateBackendData,
						SubPacket = new GenericSubPackets.UpdateBackendData()
						{
							ExpectedPlayers = hostController.AliveTransitPlayers
						}
					};
					Singleton<FikaServer>.Instance.SendDataToAll(ref backendPacket, DeliveryMethod.ReliableOrdered);
				}
			}

			GenericPacket genericPacket = new()
			{
				NetId = player.NetId,
				Type = EGenericSubPacketType.ClientExtract
			};

			try // This is to allow clients to extract if they lose connection
			{
				if (!isServer)
				{
					Singleton<FikaClient>.Instance.SendData(ref genericPacket, DeliveryMethod.ReliableOrdered);
				}
				else
				{
					Singleton<FikaServer>.Instance.SendDataToAll(ref genericPacket, DeliveryMethod.ReliableOrdered);
					ClearHostAI(player);
				}
			}
			catch
			{

			}

			if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
			{
				CoopPlayer coopPlayer = player;
				ExtractedPlayers.Add(coopPlayer.NetId);
				coopHandler.ExtractedPlayers.Add(coopPlayer.NetId);
				coopHandler.Players.Remove(coopPlayer.NetId);

				preloaderUI.StartBlackScreenShow(2f, 2f, () =>
				{
					preloaderUI.FadeBlackScreen(2f, -2f);
				});

				player.ActiveHealthController.SetDamageCoeff(0);
				player.ActiveHealthController.DamageMultiplier = 0;
				player.ActiveHealthController.DisableMetabolism();
				player.ActiveHealthController.PauseAllEffects();

				extractRoutine = StartCoroutine(ExtractRoutine(player));

				// Prevents players from looting after extracting
				CurrentScreenSingleton.Instance.CloseAllScreensForced();

				// Detroys session timer
				if (timeManager != null)
				{
					Destroy(timeManager);
				}
				if (GameUi.TimerPanel.enabled)
				{
					GameUi.TimerPanel.Close();
				}

				if (FikaPlugin.AutoExtract.Value || FikaBackendUtils.IsTransit)
				{
					if (!isServer)
					{
						Stop(coopHandler.MyPlayer.ProfileId, ExitStatus, coopHandler.MyPlayer.ActiveHealthController.IsAlive ? ExitLocation : null, 0);
					}
					else if (Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount == 0)
					{
						Stop(coopHandler.MyPlayer.ProfileId, ExitStatus, coopHandler.MyPlayer.ActiveHealthController.IsAlive ? ExitLocation : null, 0);
					}
				}
			}
			else
			{
				throw new NullReferenceException("Extract: CoopHandler was null!");
			}
		}

		/// <summary>
		/// Used to make sure no stims or mods reset the DamageCoeff
		/// </summary>
		/// <param name="player">The <see cref="CoopPlayer"/> to run the coroutine on</param>
		/// <returns></returns>
		private IEnumerator ExtractRoutine(CoopPlayer player)
		{
			while (Status != GameStatus.Stopping)
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
			if (botsController_0 != null)
			{
				botsController_0.DestroyInfo(player);
			}
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
			if (timeManager != null)
			{
				Destroy(timeManager);
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
			if (exitStatus < ExitStatus.Transit)
			{
				FikaBackendUtils.IsTransit = false;
			}

			if (FikaBackendUtils.IsTransit)
			{
				GClass1348 data = FikaBackendUtils.TransitData;
				data.isLocationTransition = true;
				data.transitionCount++;
				data.visitedLocations = [.. data.visitedLocations, Location_0.Id];
				FikaBackendUtils.TransitData = data;
			}
			else
			{
				FikaBackendUtils.ResetTransitData();
			}

			NetworkTimeSync.Reset();
			Logger.LogDebug("Stop");

			ToggleDebug(false);

			if (fikaDebug != null)
			{
				ToggleDebug(false);
				Destroy(fikaDebug);
				fikaDebug = null;
			}

			CoopPlayer myPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
			myPlayer.PacketSender.DestroyThis();

			if (myPlayer.Side != EPlayerSide.Savage)
			{
				if (myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem != null)
				{
					GStruct446<GClass3131> result = InteractionsHandlerClass.Remove(myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem,
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

			if (isServer)
			{
				Destroy(botStateManager);
				if (GameWorld_0.ServerShellingController != null)
				{
					UpdateByUnity -= GameWorld_0.ServerShellingController.OnUpdate;
				}
				botsController_0.Stop();
				bossSpawnScenario?.Stop();
				if (nonWavesSpawnScenario_0 != null)
				{
					nonWavesSpawnScenario_0.Stop();
				}
				if (wavesSpawnScenario_0 != null)
				{
					wavesSpawnScenario_0.Stop();
				}
			}

			if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
			{
				// Create a copy to prevent errors when the dictionary is being modified (which happens when using spawn mods)
				CoopPlayer[] players = [.. coopHandler.Players.Values];
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
				Destroy(coopHandler);
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

			exfilManager.Stop();

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

			TimeSpan playTimeDuration = EFTDateTimeClass.Now - dateTime_0;

			GClass1924 parameters = new()
			{
				profile = new GClass1962(this.Profile_0, GClass1971.Instance).ToUnparsedData(),
				result = exitStatus,
				killerId = gparam_0.Player.KillerId,
				killerAid = gparam_0.Player.KillerAccountId,
				exitName = exitName,
				inSession = true,
				favorite = (Profile_0.Info.Side == EPlayerSide.Savage),
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

			hasSaved = true;
		}

		public Dictionary<string, GClass1301[]> GetOwnSentItems(string profileId)
		{
			GameWorld instance = Singleton<GameWorld>.Instance;
			Dictionary<string, GClass1301[]> dictionary = [];
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
					GClass1636 transferItemsController = controller.TransferItemsController;
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
					GStruct446<GClass3131> result = InteractionsHandlerClass.Remove(myPlayer.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem,
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

			Destroy(coopHandler);

			if (isServer)
			{
				bossSpawnScenario?.Stop();
				if (nonWavesSpawnScenario_0 != null)
				{
					nonWavesSpawnScenario_0.Stop();
				}
				if (wavesSpawnScenario_0 != null)
				{
					wavesSpawnScenario_0.Stop();
				}
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
			MonoBehaviourSingleton<PreloaderUI>.Instance.StartBlackScreenShow(1f, 1f, stopManager.ExitOverride);
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
			using (CounterCreatorAbstractClass.StartWithToken("CollectMetrics"))
			{
				gclass2385_0.Stop();
			}
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
					Debug.LogException(ex);
				}
			}
			dictionary_0.Clear();
			ThrownGrenades.Clear();

			if (extractRoutine != null)
			{
				StopCoroutine(extractRoutine);
			}

			if (isServer)
			{
				CoopPlayer coopPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
				coopPlayer.PacketSender.DestroyThis();

				if (DynamicAI != null)
				{
					Destroy(DynamicAI);
				}

				if (!FikaBackendUtils.IsTransit)
				{
					NetManagerUtils.StopPinger();
				}

				FikaPlugin.DynamicAI.SettingChanged -= DynamicAI_SettingChanged;
				FikaPlugin.DynamicAIRate.SettingChanged -= DynamicAIRate_SettingChanged;
			}

			CleanUpFikaBackendUtils();

			BTRSide_Patches.Passengers.Clear();
		}

		private void CleanUpFikaBackendUtils()
		{
			if (!FikaBackendUtils.IsTransit)
			{
				FikaBackendUtils.HostExpectedNumberOfPlayers = 1;
				FikaBackendUtils.IsSpectator = false;
			}

			FikaBackendUtils.RequestFikaWorld = false;
			FikaBackendUtils.IsReconnect = false;
			FikaBackendUtils.ReconnectPosition = Vector3.zero;
		}

		private class ExitManager : Class1491
		{
			public new CoopGame baseLocalGame_0;

			public void ExitOverride()
			{
				baseLocalGame_0.GameUi.TimerPanel.Close();

				if (baseLocalGame_0.gparam_0 != null)
				{
					baseLocalGame_0.gparam_0.vmethod_1();
				}

				CurrentScreenSingleton.Instance.CloseAllScreensForced();

				//If we haven't saved, run the original method and stop running here.
				if (!baseLocalGame_0.hasSaved)
				{
					baseLocalGame_0.gparam_0.Player.TriggerZones.Clear();
					foreach (string triggerZone in baseLocalGame_0.localTriggerZones)
					{
						baseLocalGame_0.gparam_0.Player.TriggerZones.Add(triggerZone);
					}
					baseLocalGame_0.method_14(profileId, exitStatus, exitName, delay).HandleExceptions();
					return;
				}

				//Most of this is from method_14, minus the saving player part.
				baseLocalGame_0.gparam_0.Player.OnGameSessionEnd(exitStatus, baseLocalGame_0.PastTime, baseLocalGame_0.Location_0.Id, exitName);
				baseLocalGame_0.CleanUp();
				baseLocalGame_0.Dispose();

				Class1492 exitCallback = new()
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
		private class CancelExitManager : Class1491
		{
			public void ExitOverride()
			{
				CurrentScreenSingleton instance = CurrentScreenSingleton.Instance;
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
				baseLocalGame_0.method_14(profileId, exitStatus, exitName, delay).HandleExceptions();
			}
		}

		public new void method_7(string backendUrl, string locationId, int variantId)
		{
			Logger.LogDebug("method_7");
			return;
		}

		public byte[] GetHostLootItems()
		{
			if (HostLootItems == null || HostLootItems.Length == 0)
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

				return SimpleZlib.CompressToBytes(list.ToJson([]), 6);
			}

			return HostLootItems;
		}

		private int LootCompare(LootItemPositionClass a, LootItemPositionClass b)
		{
			return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
		}

		private LootItemPositionClass SerializeLootItem(LootItem lootItem, GameWorld gameWorld)
		{
			short num = -1;
			if (gameWorld.Platforms.Length != 0 && lootItem.Platform != null)
			{
				num = (short)Array.IndexOf(gameWorld.Platforms, lootItem.Platform);
			}
			/*Corpse corpse;*/
			if (lootItem is Corpse or ObservedCorpse)
			{
				return null;
			}
			LootItemPositionClass lootItemPositionClass;
			// TODO: Send corpses instead of killing the players...
			/*if ((corpse = lootItem as Corpse) != null)
			{
				lootItemPositionClass = new CorpseLootItemClass
				{
					Customization = corpse.Customization,
					Side = corpse.Side,
					Bones = ((num > -1) ? corpse.TransformSyncsRelativeToPlatform : corpse.TransformSyncs),
					ProfileID = corpse.PlayerProfileID
				};
			}
			else
			{
				lootItemPositionClass = new LootItemPositionClass();
			}*/
			lootItemPositionClass = new LootItemPositionClass();
			Transform transform = lootItem.transform;
			lootItemPositionClass.Position = ((num > -1) ? transform.localPosition : transform.position);
			lootItemPositionClass.Rotation = ((num > -1) ? transform.localRotation.eulerAngles : transform.rotation.eulerAngles);
			lootItemPositionClass.Item = lootItem.ItemOwner.RootItem;
			lootItemPositionClass.ValidProfiles = lootItem.ValidProfiles;
			lootItemPositionClass.Id = lootItem.StaticId;
			lootItemPositionClass.IsContainer = lootItem.StaticId != null;
			lootItemPositionClass.Shift = lootItem.Shift;
			lootItemPositionClass.PlatformId = num;
			return lootItemPositionClass;
		}

		public void SetClientTime(DateTime gameTime, TimeSpan sessionTime)
		{
			this.gameTime = gameTime;
			this.sessionTime = sessionTime;
		}
	}
}
