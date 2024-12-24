using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Console;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.FreeCamera.Patches;
using Fika.Core.Coop.Patches;
using Fika.Core.Coop.Patches.Camera;
using Fika.Core.Coop.Patches.Lighthouse;
using Fika.Core.Coop.Patches.SPTBugs;
using Fika.Core.EssentialPatches;
using Fika.Core.Models;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Websocket;
using Fika.Core.UI;
using Fika.Core.UI.Models;
using Fika.Core.UI.Patches;
using Fika.Core.UI.Patches.MatchmakerAcceptScreen;
using Fika.Core.Utils;
using SPT.Common.Http;
using SPT.Custom.Patches;
using SPT.Custom.Utils;
using SPT.SinglePlayer.Patches.MainMenu;
using SPT.SinglePlayer.Patches.RaidFix;
using SPT.SinglePlayer.Patches.ScavMode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core
{
	/// <summary>
	/// Fika.Core Plugin. <br/> <br/>
	/// Originally by: Paulov <br/>
	/// Re-written by: Lacyway & the Fika team
	/// </summary>
	[BepInPlugin("com.fika.core", "Fika.Core", FikaVersion)]
	[BepInProcess("EscapeFromTarkov.exe")]
	[BepInDependency("com.SPT.custom", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-custom, that way we can disable its patches
	[BepInDependency("com.SPT.singleplayer", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-singleplayer, that way we can disable its patches
	[BepInDependency("com.SPT.core", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-core, that way we can disable its patches
	[BepInDependency("com.SPT.debugging", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-debugging, that way we can disable its patches
	public class FikaPlugin : BaseUnityPlugin
	{
		public const string FikaVersion = "1.1.0";
		public static FikaPlugin Instance;
		public static InternalBundleLoader BundleLoaderPlugin { get; private set; }
		public static string EFTVersionMajor { get; internal set; }
		public static string ServerModVersion { get; private set; }
		public ManualLogSource FikaLogger
		{
			get
			{
				return Logger;
			}
		}
		public BotDifficulties BotDifficulties;
		public FikaModHandler ModHandler = new();
		public string[] LocalIPs;
		public IPAddress WanIP;
		public bool LocalesLoaded;

		private static readonly Version RequiredServerVersion = new("2.3.2");

		public static DedicatedRaidWebSocketClient DedicatedRaidWebSocket { get; set; }

		public static Dictionary<string, string> RespectedPlayersList = new()
		{
			{ "samswat",      "godfather of modern SPT modding ~ SSH"                                                       },
			{ "katto",        "kmc leader & founder. OG revolutionary of custom assets ~ SSH"                               },
			{ "polivilas",    "who started it all -- #emutarkov2019 ~ Senko-san"                                            },
			{ "balist0n",     "author of the first singleplayer-focussed mechanics and good friend ~ Senko-san"             },
			{ "ghostfenixx",  "keeps asking me to fix bugs ~ TheSparta"                                                     },
			{ "thurman",      "aka TwistedGA, helped a lot of new modders, including me when I first started ~ TheSparta"   },
			{ "chomp",        "literally unstoppable, carrying SPT development every single day ~ TheSparta"                },
			{ "nimbul",       "Sat with Lacy many night and is loved by both Lacy & me. We miss you <3 ~ SSH"               },
			{ "vox",          "My favourite american. ~ Lacyway"                                                            },
			{ "rairai",       "Very nice and caring person, someone I've appreciated getting to know. ~ Lacyway"            },
			{ "cwx",          "Active and dedicated tester who has contributed a lot of good ideas to Fika. ~ Lacyway"      }
		};

		public static Dictionary<string, string> DevelopersList = new()
		{
			{ "lacyway",      "no one unified the community as much as you ~ Senko-san"                  },
			{ "ssh_",         "my little favorite gremlin. ~ Lacyway"                                    },
			{ "nexus4880",    "the one who taught me everything I know now. ~ SSH"                       },
			{ "thesparta",    "I keep asking him to fix these darn bugs ~ GhostFenixx"                   },
			{ "senko-san",    "creator of SPT, extremely talented dev, a blast to work with ~ TheSparta" },
			{ "leaves",       "Super talented person who comes up with the coolest ideas ~ Lacyway"      },
			{ "Archangel",    "The 'tbh' guy :pepeChad: ~ Lacyway"                                       },
			{ "trippy",       "One of the chads that made the dedicated client a reality ~ Archangel"    }
		};

		#region config values

		// Hidden
		public static ConfigEntry<bool> AcceptedTOS { get; set; }

		//Advanced
		public static ConfigEntry<bool> OfficialVersion { get; set; }

		// Coop
		public static ConfigEntry<bool> ShowNotifications { get; set; }
		public static ConfigEntry<bool> AutoExtract { get; set; }
		public static ConfigEntry<bool> ShowExtractMessage { get; set; }
		public static ConfigEntry<KeyboardShortcut> ExtractKey { get; set; }
		public static ConfigEntry<bool> EnableChat { get; set; }
		public static ConfigEntry<KeyboardShortcut> ChatKey { get; set; }
		public static ConfigEntry<bool> EnableOnlinePlayers { get; set; }
		public static ConfigEntry<float> OnlinePlayersScale { get; set; }

		// Coop | Name Plates
		public static ConfigEntry<bool> UseNamePlates { get; set; }
		public static ConfigEntry<bool> HideHealthBar { get; set; }
		public static ConfigEntry<bool> UseHealthNumber { get; set; }
		public static ConfigEntry<bool> UsePlateFactionSide { get; set; }
		public static ConfigEntry<bool> HideNamePlateInOptic { get; set; }
		public static ConfigEntry<bool> NamePlateUseOpticZoom { get; set; }
		public static ConfigEntry<bool> DecreaseOpacityNotLookingAt { get; set; }
		public static ConfigEntry<float> NamePlateScale { get; set; }
		public static ConfigEntry<float> OpacityInADS { get; set; }
		public static ConfigEntry<float> MaxDistanceToShow { get; set; }
		public static ConfigEntry<float> MinimumOpacity { get; set; }
		public static ConfigEntry<float> MinimumNamePlateScale { get; set; }
		public static ConfigEntry<bool> ShowEffects { get; set; }
		public static ConfigEntry<bool> UseOcclusion { get; set; }

		// Coop | Quest Sharing
		public static ConfigEntry<EQuestSharingTypes> QuestTypesToShareAndReceive { get; set; }
		public static ConfigEntry<bool> QuestSharingNotifications { get; set; }
		public static ConfigEntry<bool> EasyKillConditions { get; set; }
		public static ConfigEntry<bool> SharedKillExperience { get; set; }
		public static ConfigEntry<bool> SharedBossExperience { get; set; }

		// Coop | Pinging
		public static ConfigEntry<bool> UsePingSystem { get; set; }
		public static ConfigEntry<KeyboardShortcut> PingButton { get; set; }
		public static ConfigEntry<Color> PingColor { get; set; }
		public static ConfigEntry<float> PingSize { get; set; }
		public static ConfigEntry<int> PingTime { get; set; }
		public static ConfigEntry<bool> PlayPingAnimation { get; set; }
		public static ConfigEntry<bool> ShowPingDuringOptics { get; set; }
		public static ConfigEntry<bool> PingUseOpticZoom { get; set; }
		public static ConfigEntry<bool> PingScaleWithDistance { get; set; }
		public static ConfigEntry<float> PingMinimumOpacity { get; set; }
		public static ConfigEntry<bool> ShowPingRange { get; set; }
		public static ConfigEntry<EPingSound> PingSound { get; set; }

		// Coop | Debug
		public static ConfigEntry<KeyboardShortcut> FreeCamButton { get; set; }
		public static ConfigEntry<bool> AllowSpectateBots;
		public static ConfigEntry<bool> AZERTYMode { get; set; }
		public static ConfigEntry<bool> DroneMode { get; set; }
		public static ConfigEntry<bool> KeybindOverlay { get; set; }

		// Performance
		public static ConfigEntry<bool> DynamicAI { get; set; }
		public static ConfigEntry<float> DynamicAIRange { get; set; }
		public static ConfigEntry<EDynamicAIRates> DynamicAIRate { get; set; }
		public static ConfigEntry<bool> DynamicAIIgnoreSnipers { get; set; }

		// Performance | Bot Limits            
		public static ConfigEntry<bool> EnforcedSpawnLimits { get; set; }
		public static ConfigEntry<bool> DespawnFurthest { get; set; }
		public static ConfigEntry<float> DespawnMinimumDistance { get; set; }
		public static ConfigEntry<int> MaxBotsFactory { get; set; }
		public static ConfigEntry<int> MaxBotsCustoms { get; set; }
		public static ConfigEntry<int> MaxBotsInterchange { get; set; }
		public static ConfigEntry<int> MaxBotsReserve { get; set; }
		public static ConfigEntry<int> MaxBotsGroundZero { get; set; }
		public static ConfigEntry<int> MaxBotsWoods { get; set; }
		public static ConfigEntry<int> MaxBotsStreets { get; set; }
		public static ConfigEntry<int> MaxBotsShoreline { get; set; }
		public static ConfigEntry<int> MaxBotsLabs { get; set; }
		public static ConfigEntry<int> MaxBotsLighthouse { get; set; }

		// Network
		public static ConfigEntry<bool> NativeSockets { get; set; }
		public static ConfigEntry<string> ForceIP { get; set; }
		public static ConfigEntry<string> ForceBindIP { get; set; }
		public static ConfigEntry<int> UDPPort { get; set; }
		public static ConfigEntry<bool> UseUPnP { get; set; }
		public static ConfigEntry<bool> UseNatPunching { get; set; }
		public static ConfigEntry<int> ConnectionTimeout { get; set; }
		public static ConfigEntry<ESendRate> SendRate { get; set; }
		public static ConfigEntry<ESmoothingRate> SmoothingRate { get; set; }

		// Gameplay
		public static ConfigEntry<bool> DisableBotMetabolism { get; set; }
		#endregion

		#region client config
		public bool UseBTR;
		public bool FriendlyFire;
		public bool DynamicVExfils;
		public bool AllowFreeCam;
		public bool AllowSpectateFreeCam;
		public bool AllowItemSending;
		public string[] BlacklistedItems;
		public bool ForceSaveOnDeath;
		public bool UseInertia;
		public bool SharedQuestProgression;
		public bool CanEditRaidSettings;
		public bool EnableTransits;
		#endregion

		#region natpunch config
		public bool NatPunchServerEnable;
		public string NatPunchServerIP;
		public int NatPunchServerPort;
		public int NatPunchServerNatIntroduceAmount;
		#endregion

		protected void Awake()
		{
			Instance = this;

			GetNatPunchServerConfig();
			EnableFikaPatches();
			EnableTranspilers();
			gameObject.AddComponent<MainThreadDispatcher>();

#if GOLDMASTER
            new TOS_Patch().Enable();
#endif

			DisableSPTPatches();
			FixSPTBugPatches();
			EnableOverridePatches();

			GetClientConfig();

			string fikaVersion = Assembly.GetAssembly(typeof(FikaPlugin)).GetName().Version.ToString();

			Logger.LogInfo($"Fika is loaded! Running version: " + fikaVersion);

			BundleLoaderPlugin = new();
			BundleLoaderPlugin.Create();

			GClass759.Init();

			BotDifficulties = FikaRequestHandler.GetBotDifficulties();
			ConsoleScreen.Processor.RegisterCommandGroup<FikaCommands>();

			if (AllowItemSending)
			{
				new ItemContext_Patch().Enable();
			}

			StartCoroutine(RunChecks());
		}

		private void SetupConfigEventHandlers()
		{
			OfficialVersion.SettingChanged += OfficialVersion_SettingChanged;
		}

		private static void EnableFikaPatches()
		{
			new FikaVersionLabel_Patch().Enable();
			new TarkovApplication_method_18_Patch().Enable();
			new DisableReadyButton_Patch().Enable();
			new DisableInsuranceReadyButton_Patch().Enable();
			new DisableMatchSettingsReadyButton_Patch().Enable();
			new TarkovApplication_LocalGamePreparer_Patch().Enable();
			new TarkovApplication_LocalGameCreator_Patch().Enable();
			new DeathFade_Patch().Enable();
			new NonWaveSpawnScenario_Patch().Enable();
			new WaveSpawnScenario_Patch().Enable();
			new MatchmakerAcceptScreen_Awake_Patch().Enable();
			new MatchmakerAcceptScreen_Show_Patch().Enable();
			new Minefield_method_2_Patch().Enable();
			new MineDirectional_OnTriggerEnter_Patch().Enable();
			new BotCacher_Patch().Enable();
			new AbstractGame_InRaid_Patch().Enable();
			new DisconnectButton_Patch().Enable();
			new ChangeGameModeButton_Patch().Enable();
			new MenuTaskBar_Patch().Enable();
			new GameWorld_Create_Patch().Enable();
			new BTRControllerClass_Init_Patch().Enable();
			new BTRView_SyncViewFromServer_Patch().Enable();
			new BTRView_GoIn_Patch().Enable();
			new BTRView_GoOut_Patch().Enable();
			new BTRVehicle_method_38_Patch().Enable();
			new Player_Hide_Patch().Enable();
			new Player_UpdateBtrTraderServiceData_Patch().Enable();
			BTRSide_Patches.Enable();
			new GClass2406_UpdateOfflineClientLogic_Patch().Enable();
			new GClass2407_UpdateOfflineClientLogic_Patch().Enable();
			new GClass2400_GetSyncObjectStrategyByType_Patch().Enable();
			LighthouseTraderZone_Patches.Enable();
			Zyriachy_Patches.Enable();
			new BufferZoneControllerClass_method_1_Patch().Enable();
			new BufferZoneControllerClass_SetPlayerInZoneStatus_Patch().Enable();
			new BufferInnerZone_ChangeZoneInteractionAvailability_Patch().Enable();
			new BufferInnerZone_ChangePlayerAccessStatus_Patch().Enable();
			new TripwireSynchronizableObject_method_6_Patch().Enable();
			new TripwireSynchronizableObject_method_11_Patch().Enable();
			new BaseLocalGame_method_13_Patch().Enable();
			new Player_OnDead_Patch().Enable();
			new Player_ManageAggressor_Patch().Enable();
			new Player_SetDogtagInfo_Patch().Enable();
			new WeaponManagerClass_ValidateScopeSmoothZoomUpdate_Patch().Enable();
			new WeaponManagerClass_method_12_Patch().Enable();
			new OpticRetrice_UpdateTransform_Patch().Enable();
			new MatchmakerOfflineRaidScreen_Close_Patch().Enable();
			new BodyPartCollider_SetUpPlayer_Patch().Enable();
			new MatchmakerOfflineRaidScreen_Show_Patch().Enable();
			new RaidSettingsWindow_method_8_Patch().Enable();
			new AIPlaceLogicPartisan_Dispose_Patch().Enable();
			new Player_SpawnInHands_Patch().Enable();
			new GClass607_method_0_Patch().Enable();
			new GClass607_method_30_Patch().Enable();
			new GClass1350_Constructor_Patch().Enable();
			new AchievementsScreen_Show_Patch().Enable();
			new AchievementView_Show_Patch().Enable();
			new GClass3299_ExceptAI_Patch().Enable();
			new GClass1641_method_8_Patch().Enable();
			new GrenadeClass_Init_Patch().Enable();
			new SessionResultExitStatus_Show_Patch().Enable();
			new PlayUISound_Patch().Enable();
			new PlayEndGameSound_Patch().Enable();
			new MenuScreen_Awake_Patch().Enable();
			new GClass3511_ShowAction_Patch().Enable();
			new MenuScreen_method_8_Patch().Enable();
			new HideoutPlayerOwner_SetPointOfView_Patch().Enable();
			new RagfairScreen_Show_Patch().Enable();
			new MatchmakerPlayerControllerClass_GetCoopBlockReason_Patch().Enable();
			new CoopSettingsWindow_Show_Patch().Enable();
			new MainMenuController_method_49_Patch().Enable();
			new GameWorld_ThrowItem_Patch().Enable();
			new RaidSettingsWindow_Show_Patch().Enable();
			new TransitControllerAbstractClass_Exist_Patch().Enable();
			new BotReload_method_1_Patch().Enable();
			new Class1374_ReloadBackendLocale_Patch().Enable();
			new GClass2013_method_0_Patch().Enable();
#if DEBUG
			TasksExtensions_HandleFinishedTask_Patches.Enable();
			new GClass1640_method_0_Patch().Enable();
			new TestHalloweenPatch().Enable();
#endif
		}

		private void EnableTranspilers()
		{
			new BotOwner_UpdateManual_Transpiler().Enable();
			new CoverPointMaster_method_0_Transpiler().Enable();
		}

		private void VerifyServerVersion()
		{
			string version = FikaRequestHandler.CheckServerVersion().Version;
			bool failed = true;
			if (Version.TryParse(version, out Version serverVersion))
			{
				if (serverVersion >= RequiredServerVersion)
				{
					failed = false;
				}
			}

			if (failed)
			{
				FikaLogger.LogError($"Server version check failed. Expected: >{RequiredServerVersion}, received: {serverVersion}");
				MessageBoxHelper.Show($"Failed to verify server mod version.\nMake sure that the server mod is installed and up-to-date!\nRequired Server Version: {RequiredServerVersion}",
					"FIKA ERROR", MessageBoxHelper.MessageBoxType.OK);
				Application.Quit();
			}
			else
			{
				FikaLogger.LogInfo($"Server version check passed. Expected: >{RequiredServerVersion}, received: {serverVersion}");
			}
		}

		/// <summary>
		/// Coroutine to ensure all mods are loaded by waiting 5 seconds
		/// </summary>
		/// <returns></returns>
		private IEnumerator RunChecks()
		{
			Task<IPAddress> addressTask = FikaRequestHandler.GetPublicIP();
			while (!addressTask.IsCompleted)
			{
				yield return null;
			}
			WanIP = addressTask.Result;

			yield return new WaitForSeconds(5);
			VerifyServerVersion();
			ModHandler.VerifyMods();
		}

		private void GetClientConfig()
		{
			ClientConfigModel clientConfig = FikaRequestHandler.GetClientConfig();

			UseBTR = clientConfig.UseBTR;
			FriendlyFire = clientConfig.FriendlyFire;
			DynamicVExfils = clientConfig.DynamicVExfils;
			AllowFreeCam = clientConfig.AllowFreeCam;
			AllowSpectateFreeCam = clientConfig.AllowSpectateFreeCam;
			AllowItemSending = clientConfig.AllowItemSending;
			BlacklistedItems = clientConfig.BlacklistedItems;
			ForceSaveOnDeath = clientConfig.ForceSaveOnDeath;
			UseInertia = clientConfig.UseInertia;
			SharedQuestProgression = clientConfig.SharedQuestProgression;
			CanEditRaidSettings = clientConfig.CanEditRaidSettings;
			EnableTransits = clientConfig.EnableTransits;

			clientConfig.LogValues();
		}

		private void GetNatPunchServerConfig()
		{
			NatPunchServerConfigModel natPunchServerConfig = FikaRequestHandler.GetNatPunchServerConfig();

			NatPunchServerEnable = natPunchServerConfig.Enable;
			NatPunchServerIP = RequestHandler.Host.Replace("http://", "").Split(':')[0];
			NatPunchServerPort = natPunchServerConfig.Port;
			NatPunchServerNatIntroduceAmount = natPunchServerConfig.NatIntroduceAmount;

			natPunchServerConfig.LogValues();
		}

		/// <summary>
		/// This is required for the locales to be properly loaded, for some reason they are still unavailable for a few seconds after getting populated
		/// </summary>
		/// <param name="__result">The <see cref="Task"/> that populates the locales</param>
		/// <returns></returns>
		public IEnumerator WaitForLocales(Task __result)
		{
			Logger.LogInfo("Waiting for locales to be ready...");
			while (!__result.IsCompleted)
			{
				yield return null;
			}
			while (LocaleUtils.BEPINEX_H_ADVANCED.Localized() == "F_BepInEx_H_Advanced")
			{
				yield return new WaitForSeconds(1);
			}
			LocalesLoaded = true;
			Logger.LogInfo("Locales are ready!");
			SetupConfig();
			FikaVersionLabel_Patch.UpdateVersionLabel();
		}

		private string CleanConfigString(string header)
		{
			string original = string.Copy(header);
			string[] forbiddenChars = ["\n", "\t", "\\", "\"", "'", "[", "]"];
			foreach (string character in forbiddenChars)
			{
				if (header.Contains(character))
				{
					FikaLogger.LogWarning($"Header {original} contains an illegal character: {character}\nReport this to the developers!");
					header.Replace(character, "");
				}
			}

			return header;
		}

		private void SetupConfig()
		{
			// Hidden

			AcceptedTOS = Config.Bind("Hidden", "Accepted TOS", false,
				new ConfigDescription("Has accepted TOS", tags: new ConfigurationManagerAttributes() { Browsable = false }));

			// Advanced

			FikaLogger.LogInfo("Setting up Advanced section");

			OfficialVersion = Config.Bind(CleanConfigString(LocaleUtils.BEPINEX_H_ADVANCED.Localized()), LocaleUtils.BEPINEX_OFFICIAL_VERSION_T.Localized(), false,
				new ConfigDescription(LocaleUtils.BEPINEX_OFFICIAL_VERSION_D.Localized(), tags: new ConfigurationManagerAttributes() { IsAdvanced = true }));

			// Coop

			FikaLogger.LogInfo("Setting up Coop section");

			string coopHeader = CleanConfigString(LocaleUtils.BEPINEX_H_COOP.Localized());

			ShowNotifications = Instance.Config.Bind(coopHeader, CleanConfigString(LocaleUtils.BEPINEX_SHOW_FEED_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_SHOW_FEED_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 7 }));

			AutoExtract = Config.Bind(coopHeader, CleanConfigString(LocaleUtils.BEPINEX_AUTO_EXTRACT_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_AUTO_EXTRACT_D.Localized(),
				tags: new ConfigurationManagerAttributes() { Order = 6 }));

			ShowExtractMessage = Config.Bind(coopHeader, CleanConfigString(LocaleUtils.BEPINEX_SHOW_EXTRACT_MESSAGE_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_SHOW_EXTRACT_MESSAGE_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 5 }));

			ExtractKey = Config.Bind(coopHeader, CleanConfigString(LocaleUtils.BEPINEX_EXTRACT_KEY_T.Localized()), new KeyboardShortcut(KeyCode.F8),
				new ConfigDescription(LocaleUtils.BEPINEX_EXTRACT_KEY_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 4 }));

			EnableChat = Config.Bind(coopHeader, CleanConfigString(LocaleUtils.BEPINEX_ENABLE_CHAT_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_ENABLE_CHAT_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 3 }));

			ChatKey = Config.Bind(coopHeader, CleanConfigString(LocaleUtils.BEPINEX_CHAT_KEY_T.Localized()), new KeyboardShortcut(KeyCode.RightControl),
				new ConfigDescription(LocaleUtils.BEPINEX_CHAT_KEY_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 2 }));

			EnableOnlinePlayers = Config.Bind(coopHeader, CleanConfigString(LocaleUtils.BEPINEX_ENABLE_ONLINE_PLAYER_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_ENABLE_ONLINE_PLAYER_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 1 }));

			OnlinePlayersScale = Config.Bind(coopHeader, CleanConfigString(LocaleUtils.BEPINEX_ONLINE_PLAYERS_SCALE_T.Localized()), 1f,
				new ConfigDescription(LocaleUtils.BEPINEX_ONLINE_PLAYERS_SCALE_D.Localized(),
				new AcceptableValueRange<float>(0.5f, 1.5f), new ConfigurationManagerAttributes() { Order = 0 }));

			// Coop | Name Plates

			FikaLogger.LogInfo("Setting up Name plates section");

			string coopNameplatesHeader = CleanConfigString(LocaleUtils.BEPINEX_H_COOP_NAME_PLATES.Localized());

			UseNamePlates = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_USE_NAME_PLATES_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_USE_NAME_PLATES_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 13 }));

			HideHealthBar = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_HIDE_HEALTH_BAR_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_HIDE_HEALTH_BAR_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 12 }));

			UseHealthNumber = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_USE_PERCENT_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_USE_PERCENT_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 11 }));

			ShowEffects = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_SHOW_EFFECTS_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_SHOW_EFFECTS_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 10 }));

			UsePlateFactionSide = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_SHOW_FACTION_ICON_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_SHOW_FACTION_ICON_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 9 }));

			HideNamePlateInOptic = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_HIDE_IN_OPTIC_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_HIDE_IN_OPTIC_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 8 }));

			NamePlateUseOpticZoom = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_OPTIC_USE_ZOOM_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_OPTIC_USE_ZOOM_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 7, IsAdvanced = true }));

			DecreaseOpacityNotLookingAt = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_DEC_OPAC_PERI_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_DEC_OPAC_PERI_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 6 }));

			NamePlateScale = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_NAME_PLATE_SCALE_T.Localized()), 0.22f,
				new ConfigDescription(LocaleUtils.BEPINEX_NAME_PLATE_SCALE_D.Localized(), new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes() { Order = 5 }));

			OpacityInADS = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_ADS_OPAC_T.Localized()), 0.75f,
				new ConfigDescription(LocaleUtils.BEPINEX_ADS_OPAC_D.Localized(), new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes() { Order = 4 }));

			MaxDistanceToShow = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_MAX_DISTANCE_T.Localized()), 500f,
				new ConfigDescription(LocaleUtils.BEPINEX_MAX_DISTANCE_D.Localized(), new AcceptableValueRange<float>(10f, 1000f), new ConfigurationManagerAttributes() { Order = 3 }));

			MinimumOpacity = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_MIN_OPAC_T.Localized()), 0.1f,
				new ConfigDescription(LocaleUtils.BEPINEX_MIN_OPAC_D.Localized(), new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes() { Order = 2 }));

			MinimumNamePlateScale = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_MIN_PLATE_SCALE_T.Localized()), 0.01f,
				new ConfigDescription(LocaleUtils.BEPINEX_MIN_PLATE_SCALE_D.Localized(), new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes() { Order = 1 }));

			UseOcclusion = Config.Bind(coopNameplatesHeader, CleanConfigString(LocaleUtils.BEPINEX_USE_OCCLUSION_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_USE_OCCLUSION_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 0 }));

			// Coop | Quest Sharing

			FikaLogger.LogInfo("Setting up Quest sharing section");

			string coopQuestSharingHeader = CleanConfigString(LocaleUtils.BEPINEX_H_COOP_QUEST_SHARING.Localized());

			QuestTypesToShareAndReceive = Config.Bind(coopQuestSharingHeader, CleanConfigString(LocaleUtils.BEPINEX_QUEST_TYPES_T.Localized()), EQuestSharingTypes.All,
				new ConfigDescription(LocaleUtils.BEPINEX_QUEST_TYPES_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 4 }));

			QuestSharingNotifications = Config.Bind(coopQuestSharingHeader, CleanConfigString(LocaleUtils.BEPINEX_QS_NOTIFICATIONS_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_QS_NOTIFICATIONS_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 3 }));

			EasyKillConditions = Config.Bind(coopQuestSharingHeader, CleanConfigString(LocaleUtils.BEPINEX_EASY_KILL_CONDITIONS_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_EASY_KILL_CONDITIONS_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 2 }));

			SharedKillExperience = Config.Bind(coopQuestSharingHeader, CleanConfigString(LocaleUtils.BEPINEX_SHARED_KILL_XP_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_SHARED_KILL_XP_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 1 }));

			SharedBossExperience = Config.Bind(coopQuestSharingHeader, CleanConfigString(LocaleUtils.BEPINEX_SHARED_BOSS_XP_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_SHARED_BOSS_XP_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 0 }));

			// Coop | Pinging

			FikaLogger.LogInfo("Setting up Pinging section");

			string coopPingingHeader = CleanConfigString(LocaleUtils.BEPINEX_H_COOP_PINGING.Localized());

			UsePingSystem = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_SYSTEM_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_SYSTEM_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 11 }));

			PingButton = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_BUTTON_T.Localized()), new KeyboardShortcut(KeyCode.Semicolon),
				new ConfigDescription(LocaleUtils.BEPINEX_PING_BUTTON_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 10 }));

			PingColor = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_COLOR_T.Localized()), Color.white,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_COLOR_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 9 }));

			PingSize = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_SIZE_T.Localized()), 1f,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_SIZE_D.Localized(), new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes() { Order = 8 }));

			PingTime = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_TIME_T.Localized()), 3,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_TIME_D.Localized(), new AcceptableValueRange<int>(2, 10), new ConfigurationManagerAttributes() { Order = 7 }));

			PlayPingAnimation = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_ANIMATION_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_ANIMATION_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 6 }));

			ShowPingDuringOptics = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_OPTICS_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_OPTICS_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 5 }));

			PingUseOpticZoom = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_OPTIC_ZOOM_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_OPTIC_ZOOM_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 4, IsAdvanced = true }));

			PingScaleWithDistance = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_SCALE_DISTANCE_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_SCALE_DISTANCE_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 3, IsAdvanced = true }));

			PingMinimumOpacity = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_MIN_OPAC_T.Localized()), 0.05f,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_MIN_OPAC_D.Localized(), new AcceptableValueRange<float>(0f, 0.5f), new ConfigurationManagerAttributes() { Order = 2, IsAdvanced = true }));

			ShowPingRange = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_RANGE_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_RANGE_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 1 }));

			PingSound = Config.Bind(coopPingingHeader, CleanConfigString(LocaleUtils.BEPINEX_PING_SOUND_T.Localized()), EPingSound.SubQuestComplete,
				new ConfigDescription(LocaleUtils.BEPINEX_PING_SOUND_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 0 }));

			// Coop | Debug

			FikaLogger.LogInfo("Setting up Debug section");

			string coopDebugHeader = CleanConfigString(LocaleUtils.BEPINEX_H_COOP_DEBUG.Localized());

			FreeCamButton = Config.Bind(coopDebugHeader, CleanConfigString(LocaleUtils.BEPINEX_FREE_CAM_BUTTON_T.Localized()), new KeyboardShortcut(KeyCode.F9),
				CleanConfigString(LocaleUtils.BEPINEX_FREE_CAM_BUTTON_D.Localized()));

			AllowSpectateBots = Config.Bind(coopDebugHeader, CleanConfigString(LocaleUtils.BEPINEX_SPECTATE_BOTS_T.Localized()), true,
				CleanConfigString(LocaleUtils.BEPINEX_SPECTATE_BOTS_D.Localized()));

			AZERTYMode = Config.Bind(coopDebugHeader, CleanConfigString(LocaleUtils.BEPINEX_AZERTY_MODE_T.Localized()), false,
				CleanConfigString(LocaleUtils.BEPINEX_AZERTY_MODE_D.Localized()));

			DroneMode = Config.Bind(coopDebugHeader, CleanConfigString(LocaleUtils.BEPINEX_DRONE_MODE_T.Localized()), false,
				LocaleUtils.BEPINEX_DRONE_MODE_D.Localized());

			KeybindOverlay = Config.Bind(coopDebugHeader, CleanConfigString(LocaleUtils.BEPINEX_KEYBIND_OVERLAY_T.Localized()), true,
				LocaleUtils.BEPINEX_KEYBIND_OVERLAY_T.Localized());

			// Performance

			FikaLogger.LogInfo("Setting up Performance section");

			string performanceHeader = CleanConfigString(LocaleUtils.BEPINEX_H_PERFORMANCE.Localized());

			DynamicAI = Config.Bind(performanceHeader, CleanConfigString(LocaleUtils.BEPINEX_DYNAMIC_AI_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_DYNAMIC_AI_T.Localized(), tags: new ConfigurationManagerAttributes() { Order = 3 }));

			DynamicAIRange = Config.Bind(performanceHeader, CleanConfigString(LocaleUtils.BEPINEX_DYNAMIC_AI_RANGE_T.Localized()), 100f,
				new ConfigDescription(LocaleUtils.BEPINEX_DYNAMIC_AI_RANGE_D.Localized(), new AcceptableValueRange<float>(150f, 1000f), new ConfigurationManagerAttributes() { Order = 2 }));

			DynamicAIRate = Config.Bind(performanceHeader, CleanConfigString(LocaleUtils.BEPINEX_DYNAMIC_AI_RATE_T.Localized()), EDynamicAIRates.Medium,
				new ConfigDescription(LocaleUtils.BEPINEX_DYNAMIC_AI_RATE_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 1 }));

			DynamicAIIgnoreSnipers = Config.Bind(performanceHeader, CleanConfigString(LocaleUtils.BEPINEX_DYNAMIC_AI_NO_SNIPERS_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_DYNAMIC_AI_NO_SNIPERS_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 0 }));

			// Performance | Max Bots

			FikaLogger.LogInfo("Setting up Max bots section");

			string performanceBotsHeader = CleanConfigString(LocaleUtils.BEPINEX_H_PERFORMANCE_BOTS.Localized());

			EnforcedSpawnLimits = Config.Bind(performanceBotsHeader, CleanConfigString(LocaleUtils.BEPINEX_ENFORCED_SPAWN_LIMITS_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_ENFORCED_SPAWN_LIMITS_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 14 }));

			DespawnFurthest = Config.Bind(performanceBotsHeader, CleanConfigString(LocaleUtils.BEPINEX_DESPAWN_FURTHEST_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_DESPAWN_FURTHEST_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 13 }));

			DespawnMinimumDistance = Config.Bind(performanceBotsHeader, CleanConfigString(LocaleUtils.BEPINEX_DESPAWN_MIN_DISTANCE_T.Localized()), 200.0f,
				new ConfigDescription(LocaleUtils.BEPINEX_DESPAWN_MIN_DISTANCE_D.Localized(), new AcceptableValueRange<float>(50f, 3000f), new ConfigurationManagerAttributes() { Order = 12 }));

			string maxBotsHeader = CleanConfigString(LocaleUtils.BEPINEX_MAX_BOTS_T.Localized());
			string maxBotsDescription = LocaleUtils.BEPINEX_MAX_BOTS_D.Localized();

			string factory = "factory4_day".Localized();
			MaxBotsFactory = Config.Bind(performanceBotsHeader, string.Format(maxBotsHeader, factory), 0,
				new ConfigDescription(string.Format(maxBotsDescription, factory), new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 11 }));

			string customs = "bigmap".Localized();
			MaxBotsCustoms = Config.Bind(performanceBotsHeader, string.Format(maxBotsHeader, customs), 0,
				new ConfigDescription(string.Format(maxBotsDescription, customs), new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 10 }));

			string interchange = "interchange".Localized();
			MaxBotsInterchange = Config.Bind(performanceBotsHeader, string.Format(maxBotsHeader, interchange), 0,
				new ConfigDescription(string.Format(maxBotsDescription, interchange), new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 8 }));

			string reserve = "rezervbase".Localized();
			MaxBotsReserve = Config.Bind(performanceBotsHeader, string.Format(maxBotsHeader, reserve), 0,
				new ConfigDescription(string.Format(maxBotsDescription, reserve), new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 7 }));

			string woods = "woods".Localized();
			MaxBotsWoods = Config.Bind(performanceBotsHeader, string.Format(maxBotsHeader, woods), 0,
				new ConfigDescription(string.Format(maxBotsDescription, woods), new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 6 }));

			string shoreline = "shoreline".Localized();
			MaxBotsShoreline = Config.Bind(performanceBotsHeader, string.Format(maxBotsHeader, shoreline), 0,
				new ConfigDescription(string.Format(maxBotsDescription, shoreline), new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 5 }));

			string streets = "tarkovstreets".Localized();
			MaxBotsStreets = Config.Bind(performanceBotsHeader, string.Format(maxBotsHeader, streets), 0,
				new ConfigDescription(string.Format(maxBotsDescription, streets), new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 4 }));

			string groundZero = "sandbox".Localized();
			MaxBotsGroundZero = Config.Bind(performanceBotsHeader, string.Format(maxBotsHeader, groundZero), 0,
				new ConfigDescription(string.Format(maxBotsDescription, groundZero), new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 3 }));

			string labs = "laboratory".Localized();
			MaxBotsLabs = Config.Bind(performanceBotsHeader, string.Format(maxBotsHeader, labs), 0,
				new ConfigDescription(string.Format(maxBotsDescription, labs), new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 2 }));

			string lighthouse = "lighthouse".Localized();
			MaxBotsLighthouse = Config.Bind(performanceBotsHeader, string.Format(maxBotsHeader, lighthouse), 0,
				new ConfigDescription(string.Format(maxBotsDescription, lighthouse), new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 1 }));

			// Network

			FikaLogger.LogInfo("Setting up Network section");

			string networkHeader = CleanConfigString(LocaleUtils.BEPINEX_H_NETWORK.Localized());

			NativeSockets = Config.Bind(networkHeader, CleanConfigString(LocaleUtils.BEPINEX_NATIVE_SOCKETS_T.Localized()), true,
				new ConfigDescription(LocaleUtils.BEPINEX_NATIVE_SOCKETS_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 9 }));

			ForceIP = Config.Bind(networkHeader, CleanConfigString(LocaleUtils.BEPINEX_FORCE_IP_T.Localized()), "",
				new ConfigDescription(LocaleUtils.BEPINEX_FORCE_IP_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 8 }));

			ForceBindIP = Config.Bind(networkHeader, CleanConfigString(LocaleUtils.BEPINEX_FORCE_BIND_IP_T.Localized()), "0.0.0.0",
				new ConfigDescription(LocaleUtils.BEPINEX_FORCE_BIND_IP_D.Localized(), new AcceptableValueList<string>(GetLocalAddresses()), new ConfigurationManagerAttributes() { Order = 7 }));

			UDPPort = Config.Bind(networkHeader, CleanConfigString(LocaleUtils.BEPINEX_UDP_PORT_T.Localized()), 25565,
				new ConfigDescription(LocaleUtils.BEPINEX_UDP_PORT_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 5 }));

			UseUPnP = Config.Bind(networkHeader, CleanConfigString(LocaleUtils.BEPINEX_USE_UPNP_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_USE_UPNP_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 4 }));

			UseNatPunching = Config.Bind(networkHeader, CleanConfigString(LocaleUtils.BEPINEX_USE_NAT_PUNCH_T.Localized()), false,
				new ConfigDescription(LocaleUtils.BEPINEX_USE_NAT_PUNCH_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 3 }));

			ConnectionTimeout = Config.Bind(networkHeader, CleanConfigString(LocaleUtils.BEPINEX_CONNECTION_TIMEOUT_T.Localized()), 15,
				new ConfigDescription(LocaleUtils.BEPINEX_CONNECTION_TIMEOUT_D.Localized(), new AcceptableValueRange<int>(5, 60), new ConfigurationManagerAttributes() { Order = 2 }));

			SendRate = Config.Bind(networkHeader, CleanConfigString(LocaleUtils.BEPINEX_SEND_RATE_T.Localized()), ESendRate.Medium,
				new ConfigDescription(LocaleUtils.BEPINEX_SEND_RATE_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 1 }));

			SmoothingRate = Config.Bind(networkHeader, CleanConfigString(LocaleUtils.BEPINEX_SMOOTHING_RATE_T.Localized()), ESmoothingRate.Medium,
				new ConfigDescription(LocaleUtils.BEPINEX_SMOOTHING_RATE_D.Localized(), tags: new ConfigurationManagerAttributes() { Order = 0 }));

			// Gameplay

			FikaLogger.LogInfo("Setting up Gameplay section");

			DisableBotMetabolism = Config.Bind(CleanConfigString(LocaleUtils.BEPINEX_H_GAMEPLAY.Localized()), CleanConfigString(LocaleUtils.BEPINEX_DISABLE_BOT_METABOLISM_T.Localized()),
				false, new ConfigDescription(LocaleUtils.BEPINEX_DISABLE_BOT_METABOLISM_D.Localized(),
				tags: new ConfigurationManagerAttributes() { Order = 1 }));

			SetupConfigEventHandlers();
		}

		private void OfficialVersion_SettingChanged(object sender, EventArgs e)
		{
			FikaVersionLabel_Patch.UpdateVersionLabel();
		}

		private string[] GetLocalAddresses()
		{
			List<string> ips = [];
			ips.Add("Disabled");
			ips.Add("0.0.0.0");

			try
			{
				foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
				{
					foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
					{
						if (!ip.IsDnsEligible)
						{
							continue;
						}

						if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							string stringIp = ip.Address.ToString();
							if (stringIp != "127.0.0.1")
							{
								ips.Add(stringIp);
							}
						}
					}
				}

				LocalIPs = ips.Skip(1).ToArray();
				string allIps = string.Join(", ", LocalIPs);
				Logger.LogInfo($"Cached local IPs: {allIps}");
				return [.. ips];
			}
			catch (Exception ex)
			{
				Instance.FikaLogger.LogError("GetLocalAddresses: " + ex.Message);
				return [.. ips];
			}
		}

		private void DisableSPTPatches()
		{
			// Disable these as they interfere with Fika
			new VersionLabelPatch().Disable();
			new AmmoUsedCounterPatch().Disable();
			new ArmorDamageCounterPatch().Disable();
			new ScavRepAdjustmentPatch().Disable();
			new GetProfileAtEndOfRaidPatch().Disable();
			new ScavExfilPatch().Disable();
			new SendPlayerScavProfileToServerAfterRaidPatch().Disable();
			new BotOwnerManualUpdatePatch().Disable();
		}

		public void FixSPTBugPatches()
		{
			if (ModHandler.SPTCoreVersion.ToString() == "3.10.0")
			{
				new FixAirdropCrashPatch().Disable();
				new FixAirdropCrashPatch_Override().Enable();
				new FixVFSDeleteFilePatch().Enable();
			}
		}

		private void EnableOverridePatches()
		{
			new ScavProfileLoad_Override().Enable();
			new OfflineRaidSettingsMenuPatch_Override().Enable();
			new GetProfileAtEndOfRaidPatch_Override().Enable();
			new FixSavageInventoryScreenPatch_Override().Enable();
		}

		public enum EDynamicAIRates
		{
			Low,
			Medium,
			High
		}

		public enum EPingSound
		{
			SubQuestComplete,
			InsuranceInsured,
			ButtonClick,
			ButtonHover,
			InsuranceItemInsured,
			MenuButtonBottom,
			ErrorMessage,
			InspectWindow,
			InspectWindowClose,
			MenuEscape,
		}

		public enum ESmoothingRate
		{
			Low,
			Medium,
			High
		}

		public enum ESendRate
		{
			[Description("Very Low")]
			VeryLow,
			Low,
			Medium,
			High
		}

		[Flags]
		public enum EQuestSharingTypes
		{
			Kills = 1,
			Item = 2,
			Location = 4,
			PlaceBeacon = 8,

			All = Kills | Item | Location | PlaceBeacon
		}
	}
}