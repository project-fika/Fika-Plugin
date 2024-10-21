using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Console;
using Fika.Core.Coop.FreeCamera.Patches;
using Fika.Core.Coop.Patches;
using Fika.Core.Coop.Patches.Camera;
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
	[BepInPlugin("com.fika.core", "Fika.Core", "1.0.0")]
	[BepInProcess("EscapeFromTarkov.exe")]
	[BepInDependency("com.SPT.custom", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-custom, that way we can disable its patches
	[BepInDependency("com.SPT.singleplayer", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-singleplayer, that way we can disable its patches
	[BepInDependency("com.SPT.core", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-core, that way we can disable its patches
	[BepInDependency("com.SPT.debugging", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-debugging, that way we can disable its patches
	public class FikaPlugin : BaseUnityPlugin
	{
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

		private static readonly Version RequiredServerVersion = new("2.3.0");

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
		public static ConfigEntry<string> ForceBindIP2 { get; set; }
		public static ConfigEntry<float> AutoRefreshRate { get; set; }
		public static ConfigEntry<int> UDPPort { get; set; }
		public static ConfigEntry<bool> UseUPnP { get; set; }
		public static ConfigEntry<bool> UseNatPunching { get; set; }
		public static ConfigEntry<int> ConnectionTimeout { get; set; }
		public static ConfigEntry<ESendRate> SendRate { get; set; }
		public static ConfigEntry<ESmoothingRate> SmoothingRate { get; set; }

		// Gameplay
		public static ConfigEntry<float> HeadDamageMultiplier { get; set; }
		public static ConfigEntry<float> ArmpitDamageMultiplier { get; set; }
		public static ConfigEntry<float> StomachDamageMultiplier { get; set; }
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
			SetupConfig();
			EnableFikaPatches();
			gameObject.AddComponent<MainThreadDispatcher>();

#if GOLDMASTER
            new TOS_Patch().Enable();
#endif
			OfficialVersion.SettingChanged += OfficialVersion_SettingChanged;

			DisableSPTPatches();
			EnableOverridePatches();

			GetClientConfig();

			string fikaVersion = Assembly.GetAssembly(typeof(FikaPlugin)).GetName().Version.ToString();

			Logger.LogInfo($"Fika is loaded! Running version: " + fikaVersion);

			BundleLoaderPlugin = new();
			BundleLoaderPlugin.Create();

			BotSettingsRepoAbstractClass.Init();

			BotDifficulties = FikaRequestHandler.GetBotDifficulties();
			ConsoleScreen.Processor.RegisterCommandGroup<FikaCommands>();

			if (AllowItemSending)
			{
				new ItemContext_Patch().Enable();
			}

			StartCoroutine(RunChecks());
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
			new GClass2335_UpdateOfflineClientLogic_Patch().Enable();
			new GClass2336_UpdateOfflineClientLogic_Patch().Enable();
			new GClass2329_GetSyncObjectStrategyByType_Patch().Enable();
			LighthouseTraderZone_Patches.Enable();
			new BufferZoneControllerClass_method_1_Patch().Enable();
			new BufferZoneControllerClass_SetPlayerInZoneStatus_Patch().Enable();
			new BufferInnerZone_ChangeZoneInteractionAvailability_Patch().Enable();
			new BufferInnerZone_ChangePlayerAccessStatus_Patch().Enable();
			new TripwireSynchronizableObject_method_6_Patch().Enable();
			new TripwireSynchronizableObject_method_11_Patch().Enable();
			new BaseLocalGame_method_13_Patch().Enable();
			new Player_method_138_Patch().Enable();
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
			new GClass596_method_0_Patch().Enable();
			new GClass596_method_30_Patch().Enable();
			new GClass1350_Constructor_Patch().Enable();
			new AchievementsScreen_Show_Patch().Enable();
			new AchievementView_Show_Patch().Enable();
			new GClass3224_IsValid_Patch().Enable();
			new GClass3223_ExceptAI_Patch().Enable();
			new GClass1616_method_8_Patch().Enable();
			new GrenadeClass_Init_Patch().Enable();
			new SessionResultExitStatus_Show_Patch().Enable();
			new PlayUISound_Patch().Enable();
			new PlayEndGameSound_Patch().Enable();
			new MenuScreen_Awake_Patch().Enable();
			new GClass3421_ShowAction_Patch().Enable();
			new MenuScreen_method_8_Patch().Enable();
			new HideoutPlayerOwner_SetPointOfView_Patch().Enable();
			new RagfairScreen_Show_Patch().Enable();
			new MatchmakerPlayerControllerClass_GetCoopBlockReason_Patch().Enable();
			new CoopSettingsWindow_Show_Patch().Enable();
			new MainMenuController_method_48_Patch().Enable();
			new GameWorld_ThrowItem_Patch().Enable();
			new RaidSettingsWindow_Show_Patch().Enable();

#if DEBUG
			TasksExtensions_HandleFinishedTask_Patches.Enable();
			new GClass1615_method_0_Patch().Enable();
#endif
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

		private void SetupConfig()
		{
			// Hidden

			AcceptedTOS = Config.Bind("Hidden", "Accepted TOS", false,
				new ConfigDescription("Has accepted TOS", tags: new ConfigurationManagerAttributes() { Browsable = false }));

			// Advanced

			OfficialVersion = Config.Bind("Advanced", "Official Version", false,
				new ConfigDescription("Show official version instead of Fika version.", tags: new ConfigurationManagerAttributes() { IsAdvanced = true }));

			// Coop

			ShowNotifications = Instance.Config.Bind("Coop", "Show Feed", true,
				new ConfigDescription("Enable custom notifications when a player dies, extracts, kills a boss, etc.", tags: new ConfigurationManagerAttributes() { Order = 6 }));

			AutoExtract = Config.Bind("Coop", "Auto Extract", false,
				new ConfigDescription("Automatically extracts after the extraction countdown. As a host, this will only work if there are no clients connected.",
				tags: new ConfigurationManagerAttributes() { Order = 5 }));

			ShowExtractMessage = Config.Bind("Coop", "Show Extract Message", true,
				new ConfigDescription("Whether to show the extract message after dying/extracting.", tags: new ConfigurationManagerAttributes() { Order = 4 }));

			ExtractKey = Config.Bind("Coop", "Extract Key", new KeyboardShortcut(KeyCode.F8),
				new ConfigDescription("The key used to extract from the raid.", tags: new ConfigurationManagerAttributes() { Order = 3 }));

			EnableChat = Config.Bind("Coop", "Enable Chat", false,
				new ConfigDescription("Toggle to enable chat in game. Cannot be change mid raid", tags: new ConfigurationManagerAttributes() { Order = 2 }));

			ChatKey = Config.Bind("Coop", "Chat Key", new KeyboardShortcut(KeyCode.RightControl),
				new ConfigDescription("The key used to open the chat window.", tags: new ConfigurationManagerAttributes() { Order = 1 }));

			OnlinePlayersScale = Config.Bind("Coop", "Online Players Scale", 1f,
				new ConfigDescription("The scale of the window that displays online players. Only change if it looks out of proportion.\n\nRequires a refresh of the main menu to take effect.",
				new AcceptableValueRange<float>(0.5f, 1.5f), new ConfigurationManagerAttributes() { Order = 0 }));

			// Coop | Name Plates

			UseNamePlates = Config.Bind("Coop | Name Plates", "Show Player Name Plates", false,
				new ConfigDescription("Toggle Health-Bars & Names.", tags: new ConfigurationManagerAttributes() { Order = 13 }));

			HideHealthBar = Config.Bind("Coop | Name Plates", "Hide Health Bar", false,
				new ConfigDescription("Completely hides the health bar.", tags: new ConfigurationManagerAttributes() { Order = 12 }));

			UseHealthNumber = Config.Bind("Coop | Name Plates", "Show HP% instead of bar", false,
				new ConfigDescription("Shows health in % amount instead of using the bar.", tags: new ConfigurationManagerAttributes() { Order = 11 }));

			ShowEffects = Config.Bind("Coop | Name Plates", "Show Effects", true,
				new ConfigDescription("If status effects should be displayed below the health bar.", tags: new ConfigurationManagerAttributes() { Order = 10 }));

			UsePlateFactionSide = Config.Bind("Coop | Name Plates", "Show Player Faction Icon", true,
				new ConfigDescription("Shows the player faction icon next to the HP bar.", tags: new ConfigurationManagerAttributes() { Order = 9 }));

			HideNamePlateInOptic = Config.Bind("Coop | Name Plates", "Hide Name Plate in Optic", true,
				new ConfigDescription("Hides the name plate when viewing through PiP scopes.", tags: new ConfigurationManagerAttributes() { Order = 8 }));

			NamePlateUseOpticZoom = Config.Bind("Coop | Name Plates", "Name Plates Use Optic Zoom", true,
				new ConfigDescription("If name plate location should be displayed using the PiP optic camera.", tags: new ConfigurationManagerAttributes() { Order = 7, IsAdvanced = true }));

			DecreaseOpacityNotLookingAt = Config.Bind("Coop | Name Plates", "Decrease Opacity In Peripheral", true,
				new ConfigDescription("Decreases the opacity of the name plates when not looking at a player.", tags: new ConfigurationManagerAttributes() { Order = 6 }));

			NamePlateScale = Config.Bind("Coop | Name Plates", "Name Plate Scale", 0.22f,
				new ConfigDescription("Size of the name plates", new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes() { Order = 5 }));

			OpacityInADS = Config.Bind("Coop | Name Plates", "Opacity in ADS", 0.75f,
				new ConfigDescription("The opacity of the name plates when aiming down sights.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes() { Order = 4 }));

			MaxDistanceToShow = Config.Bind("Coop | Name Plates", "Max Distance to Show", 500f,
				new ConfigDescription("The maximum distance at which name plates will become invisible, starts to fade at half the input value.", new AcceptableValueRange<float>(10f, 1000f), new ConfigurationManagerAttributes() { Order = 3 }));

			MinimumOpacity = Config.Bind("Coop | Name Plates", "Minimum Opacity", 0.1f,
				new ConfigDescription("The minimum opacity of the name plates.", new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes() { Order = 2 }));

			MinimumNamePlateScale = Config.Bind("Coop | Name Plates", "Minimum Name Plate Scale", 0.01f,
				new ConfigDescription("The minimum scale of the name plates.", new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes() { Order = 1 }));

			UseOcclusion = Config.Bind("Coop | Name Plates", "Use Occlusion", false,
				new ConfigDescription("Use occlusion to hide the name plate when the player is out of sight.", tags: new ConfigurationManagerAttributes() { Order = 0 }));

			// Coop | Quest Sharing

			QuestTypesToShareAndReceive = Config.Bind("Coop | Quest Sharing", "Quest Types", EQuestSharingTypes.All,
				new ConfigDescription("Which quest types to receive and send. PlaceBeacon is both markers and items.", tags: new ConfigurationManagerAttributes() { Order = 3 }));

			QuestSharingNotifications = Config.Bind("Coop | Quest Sharing", "Show Notifications", true,
				new ConfigDescription("If a notification should be shown when quest progress is shared with out.", tags: new ConfigurationManagerAttributes() { Order = 2 }));

			EasyKillConditions = Config.Bind("Coop | Quest Sharing", "Easy Kill Conditions", false,
				new ConfigDescription("Enables easy kill conditions. When this is used, any time a friendly player kills something, it treats it as if you killed it for your quests as long as all conditions are met.\nThis can be inconsistent and does not always work.", tags: new ConfigurationManagerAttributes() { Order = 1 }));

			SharedBossExperience = Config.Bind("Coop | Quest Sharing", "Shared Boss Experience", false,
				new ConfigDescription("If enabled you will receive ½ of the experience when a friendly player kills a boss", tags: new ConfigurationManagerAttributes() { Order = 0 }));

			// Coop | Pinginging

			UsePingSystem = Config.Bind("Coop | Pinging", "Ping System", false,
				new ConfigDescription("Toggle Ping System. If enabled you can receive and send pings by pressing the ping key.", tags: new ConfigurationManagerAttributes() { Order = 11 }));

			PingButton = Config.Bind("Coop | Pinging", "Ping Button", new KeyboardShortcut(KeyCode.U),
				new ConfigDescription("Button used to send pings.", tags: new ConfigurationManagerAttributes() { Order = 10 }));

			PingColor = Config.Bind("Coop | Pinging", "Ping Color", Color.white,
				new ConfigDescription("The color of your pings when displayed for other players.", tags: new ConfigurationManagerAttributes() { Order = 9 }));

			PingSize = Config.Bind("Coop | Pinging", "Ping Size", 1f,
				new ConfigDescription("The multiplier of the ping size.", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes() { Order = 8 }));

			PingTime = Config.Bind("Coop | Pinging", "Ping Time", 3,
				new ConfigDescription("How long pings should be displayed.", new AcceptableValueRange<int>(2, 10), new ConfigurationManagerAttributes() { Order = 7 }));

			PlayPingAnimation = Config.Bind("Coop | Pinging", "Play Ping Animation", false,
				new ConfigDescription("Plays the pointing animation automatically when pinging. Can interfere with gameplay.", tags: new ConfigurationManagerAttributes() { Order = 6 }));

			ShowPingDuringOptics = Config.Bind("Coop | Pinging", "Show Ping During Optics", false,
				new ConfigDescription("If pings should be displayed while aiming down an optics scope.", tags: new ConfigurationManagerAttributes() { Order = 5 }));

			PingUseOpticZoom = Config.Bind("Coop | Pinging", "Ping Use Optic Zoom", true,
				new ConfigDescription("If ping location should be displayed using the PiP optic camera.", tags: new ConfigurationManagerAttributes() { Order = 4, IsAdvanced = true }));

			PingScaleWithDistance = Config.Bind("Coop | Pinging", "Ping Scale With Distance", true,
				new ConfigDescription("If ping size should scale with distance from player.", tags: new ConfigurationManagerAttributes() { Order = 3, IsAdvanced = true }));

			PingMinimumOpacity = Config.Bind("Coop | Pinging", "Ping Minimum Opacity", 0.05f,
				new ConfigDescription("The minimum opacity of pings when looking straight at them.", new AcceptableValueRange<float>(0f, 0.5f), new ConfigurationManagerAttributes() { Order = 2, IsAdvanced = true }));

			ShowPingRange = Config.Bind("Coop | Pinging", "Show Ping Range", false,
				new ConfigDescription("Shows the range from your player to the ping if enabled.", tags: new ConfigurationManagerAttributes() { Order = 1 }));

			PingSound = Config.Bind("Coop | Pinging", "Ping Sound", EPingSound.SubQuestComplete,
				new ConfigDescription("The audio that plays on ping", tags: new ConfigurationManagerAttributes() { Order = 0 }));

			// Coop | Debug

			FreeCamButton = Config.Bind("Coop | Debug", "Free Camera Button", new KeyboardShortcut(KeyCode.F9),
				"Button used to toggle free camera.");

			AllowSpectateBots = Config.Bind("Coop | Debug", "Allow Spectating Bots", true,
				"If we should allow spectating bots if all players are dead/extracted");

			AZERTYMode = Config.Bind("Coop | Debug", "AZERTY Mode", false,
				"If free camera should use AZERTY keys for input.");

			DroneMode = Config.Bind("Coop | Debug", "Drone Mode", false,
				"If the free camera should move only along the vertical axis like a drone");

			KeybindOverlay = Config.Bind("Coop | Debug", "Keybind Overlay", true,
				"If an overlay with all free cam keybinds should show.");

			// Performance

			DynamicAI = Config.Bind("Performance", "Dynamic AI", false,
				new ConfigDescription("Use the dynamic AI system, disabling AI when they are outside of any player's range.", tags: new ConfigurationManagerAttributes() { Order = 3 }));

			DynamicAIRange = Config.Bind("Performance", "Dynamic AI Range", 100f,
				new ConfigDescription("The range at which AI will be disabled dynamically.", new AcceptableValueRange<float>(150f, 1000f), new ConfigurationManagerAttributes() { Order = 2 }));

			DynamicAIRate = Config.Bind("Performance", "Dynamic AI Rate", EDynamicAIRates.Medium,
				new ConfigDescription("How often DynamicAI should scan for the range from all players.", tags: new ConfigurationManagerAttributes() { Order = 1 }));

			DynamicAIIgnoreSnipers = Config.Bind("Performance", "Dynamic AI - Ignore Snipers", true,
				new ConfigDescription("Whether Dynamic AI should ignore sniper scavs.", tags: new ConfigurationManagerAttributes() { Order = 0 }));

			// Performance | Max Bots

			EnforcedSpawnLimits = Config.Bind("Performance | Max Bots", "Enforced Spawn Limits", false,
				new ConfigDescription("Enforces spawn limits when spawning bots, making sure to not go over the vanilla limits. This mainly takes affect when using spawn mods or anything that modifies the bot limits. Will not block spawns of special bots like bosses.", tags: new ConfigurationManagerAttributes() { Order = 14 }));

			DespawnFurthest = Config.Bind("Performance | Max Bots", "Despawn Furthest", false,
				new ConfigDescription("When enforcing spawn limits, should the furthest bot be de-spawned instead of blocking the spawn. This will make for a much more active raid on a lower Max Bots count. Helpful for weaker PCs. Will only despawn pmcs and scavs. If you don't run a dynamic spawn mod, this will however quickly exhaust the spawns on the map, making the raid very dead instead.", tags: new ConfigurationManagerAttributes() { Order = 13 }));

			DespawnMinimumDistance = Config.Bind("Performance | Max Bots", "Despawn Minimum Distance", 200.0f,
				new ConfigDescription("Don't despawn bots within this distance.", new AcceptableValueRange<float>(50f, 3000f), new ConfigurationManagerAttributes() { Order = 12 }));

			MaxBotsFactory = Config.Bind("Performance | Max Bots", "Max Bots Factory", 0,
				new ConfigDescription("Max amount of bots that can be active at the same time on Factory. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 11 }));

			MaxBotsCustoms = Config.Bind("Performance | Max Bots", "Max Bots Customs", 0,
				new ConfigDescription("Max amount of bots that can be active at the same time on Customs. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 10 }));

			MaxBotsInterchange = Config.Bind("Performance | Max Bots", "Max Bots Interchange", 0,
				new ConfigDescription("Max amount of bots that can be active at the same time on Interchange. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 8 }));

			MaxBotsReserve = Config.Bind("Performance | Max Bots", "Max Bots Reserve", 0,
				new ConfigDescription("Max amount of bots that can be active at the same time on Reserve. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 7 }));

			MaxBotsWoods = Config.Bind("Performance | Max Bots", "Max Bots Woods", 0,
				new ConfigDescription("Max amount of bots that can be active at the same time on Woods. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 6 }));

			MaxBotsShoreline = Config.Bind("Performance | Max Bots", "Max Bots Shoreline", 0,
				new ConfigDescription("Max amount of bots that can be active at the same time on Shoreline. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 5 }));

			MaxBotsStreets = Config.Bind("Performance | Max Bots", "Max Bots Streets", 0,
				new ConfigDescription("Max amount of bots that can be active at the same time on Streets of Tarkov. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 4 }));

			MaxBotsGroundZero = Config.Bind("Performance | Max Bots", "Max Bots Ground Zero", 0,
				new ConfigDescription("Max amount of bots that can be active at the same time on Ground Zero. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 3 }));

			MaxBotsLabs = Config.Bind("Performance | Max Bots", "Max Bots Labs", 0,
				new ConfigDescription("Max amount of bots that can be active at the same time on Labs. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 2 }));

			MaxBotsLighthouse = Config.Bind("Performance | Max Bots", "Max Bots Lighthouse", 0,
				new ConfigDescription("Max amount of bots that can be active at the same time on Lighthouse. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 1 }));

			// Network

			NativeSockets = Config.Bind(section: "Network", "Native Sockets", true,
				new ConfigDescription("Use NativeSockets for gameplay traffic. This uses direct socket calls for send/receive to drastically increase speed and reduce GC pressure. Only for Windows/Linux and might not always work.", tags: new ConfigurationManagerAttributes() { Order = 9 }));

			ForceIP = Config.Bind("Network", "Force IP", "",
				new ConfigDescription("Forces the server when hosting to use this IP when broadcasting to the backend instead of automatically trying to fetch it. Leave empty to disable.", tags: new ConfigurationManagerAttributes() { Order = 8 }));

			ForceBindIP = Config.Bind("Network", "Force Bind IP", "",
				new ConfigDescription("Forces the server when hosting to use this local IP when starting the server. Useful if you are hosting on a VPN.", new AcceptableValueList<string>(GetLocalAddresses()), new ConfigurationManagerAttributes() { Order = 7 }));

			AutoRefreshRate = Config.Bind("Network", "Auto Server Refresh Rate", 10f,
				new ConfigDescription("Every X seconds the client will ask the server for the list of matches while at the lobby screen.", new AcceptableValueRange<float>(3f, 60f), new ConfigurationManagerAttributes() { Order = 6 }));

			UDPPort = Config.Bind("Network", "UDP Port", 25565,
				new ConfigDescription("Port to use for UDP gameplay packets.", tags: new ConfigurationManagerAttributes() { Order = 5 }));

			UseUPnP = Config.Bind("Network", "Use UPnP", false,
				new ConfigDescription("Attempt to open ports using UPnP. Useful if you cannot open ports yourself but the router supports UPnP.", tags: new ConfigurationManagerAttributes() { Order = 4 }));

			UseNatPunching = Config.Bind("Network", "Use NAT Punching", false,
				new ConfigDescription("Use NAT punching when hosting a raid. Only works with fullcone NAT type routers and requires NatPunchServer to be running on the SPT server. UPnP, Force IP and Force Bind IP are disabled with this mode.", tags: new ConfigurationManagerAttributes() { Order = 3 }));

			ConnectionTimeout = Config.Bind("Network", "Connection Timeout", 15,
				new ConfigDescription("How long it takes for a connection to be considered dropped if no packets are received.", new AcceptableValueRange<int>(5, 60), new ConfigurationManagerAttributes() { Order = 2 }));

			SendRate = Config.Bind("Network", "Send Rate", ESendRate.Low,
				new ConfigDescription("How often per second movement packets should be sent (lower = less bandwidth used, slight more delay during interpolation)\nThis only affects the host and will be synchronized to all clients.\nAmount is per second:\n\nVery Low = 10\nLow = 20\nMedium = 40\nHigh = 60\n\nRecommended to leave at no higher than Medium as the gains are insignificant after.", tags: new ConfigurationManagerAttributes() { Order = 1 }));

			SmoothingRate = Config.Bind("Network", "Smoothing Rate", ESmoothingRate.Medium,
				new ConfigDescription("Local simulation is behind by Send Rate * Smoothing Rate. This guarantees that we always have enough snapshots in the buffer to mitigate lags & jitter during interpolation.\n\nLow = 1.5\nMedium = 2\nHigh = 2.5\n\nSet this to 'High' if movement isn't smooth. Cannot be changed during a raid.", tags: new ConfigurationManagerAttributes() { Order = 0 }));

			// Gameplay

			HeadDamageMultiplier = Config.Bind("Gameplay", "Head Damage Multiplier", 1f,
				new ConfigDescription("X multiplier to damage taken on the head collider. 0.2 = 20%", new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes() { Order = 4 }));

			ArmpitDamageMultiplier = Config.Bind("Gameplay", "Armpit Damage Multiplier", 1f,
				new ConfigDescription("X multiplier to damage taken on the armpits collider. 0.2 = 20%", new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes() { Order = 3 }));

			StomachDamageMultiplier = Config.Bind("Gameplay", "Stomach Damage Multiplier", 1f,
				new ConfigDescription("X multiplier to damage taken on the stomach collider. 0.2 = 20%", new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes() { Order = 2 }));

			DisableBotMetabolism = Config.Bind("Gameplay", "Disable Bot Metabolism", false,
				new ConfigDescription("Disables metabolism on bots, preventing them from dying from loss of energy/hydration during long raids.", tags: new ConfigurationManagerAttributes() { Order = 1 }));
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
			new FixSavageInventoryScreenPatch().Disable();
			new ScavExfilPatch().Disable();
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