using Aki.Custom.Airdrops.Patches;
using Aki.Custom.BTR.Patches;
using Aki.Custom.Patches;
using Aki.SinglePlayer.Patches.MainMenu;
using Aki.SinglePlayer.Patches.Progression;
using Aki.SinglePlayer.Patches.Quests;
using Aki.SinglePlayer.Patches.RaidFix;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT.UI;
using Fika.Core.AkiSupport.Airdrops.Utils;
using Fika.Core.AkiSupport.Overrides;
using Fika.Core.AkiSupport.Scav;
using Fika.Core.Bundles;
using Fika.Core.Console;
using Fika.Core.Coop.FreeCamera.Patches;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Coop.Patches;
using Fika.Core.Coop.Patches.LocalGame;
using Fika.Core.Coop.World;
using Fika.Core.EssentialPatches;
using Fika.Core.Models;
using Fika.Core.Networking.Http;
using Fika.Core.UI;
using Fika.Core.UI.Models;
using Fika.Core.UI.Patches;
using Fika.Core.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Fika.Core
{
    /// <summary>
    /// Fika.Core Plugin. <br/> <br/>
    /// Originally by: Paulov <br/>
    /// Re-written by: Lacyway
    /// </summary>
    [BepInPlugin("com.fika.core", "Fika.Core", "0.9.8906")]
    [BepInProcess("EscapeFromTarkov.exe")]
    [BepInDependency("com.spt-aki.custom", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after aki-custom, that way we can disable its patches
    [BepInDependency("com.spt-aki.singleplayer", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after aki-singleplayer, that way we can disable its patches
    [BepInDependency("com.spt-aki.core", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after aki-custom, that way we can disable its patches
    [BepInDependency("com.spt-aki.debugging", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after aki-custom, that way we can disable its patches
    public class FikaPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// Stores the Instance of this Plugin
        /// </summary>
        public static FikaPlugin Instance;
        public static InternalBundleLoader BundleLoaderPlugin { get; private set; }
        /// <summary>
        /// If any mod dependencies fail, show an error. This is a flag to say it has occurred.
        /// </summary>
        private bool ShownDependencyError { get; set; }
        /// <summary>
        /// This is the Official EFT Version defined by BSG
        /// </summary>
        public static string EFTVersionMajor { get; internal set; }
        public ManualLogSource FikaLogger { get => Logger; }
        public BotDifficulties BotDifficulties;
        public FikaModHandler ModHandler = new();
        public string Locale { get; private set; } = "en";
        public string[] LocalIPs;

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
            { "cwx",          "Active and dedicated tester who has contributed a lot of good ideas to Fika. ~ Lacyway"       }
        };

        public static Dictionary<string, string> DevelopersList = new()
        {
            { "lacyway",      "no one unified the community as much as you ~ Senko-san"                  },
            { "ssh_",         "my little favorite gremlin. ~ Lacyway"                                    },
            { "nexus4880",    "the one who taught me everything I know now. ~ SSH"                       },
            { "thesparta",    "I keep asking him to fix these darn bugs ~ GhostFenixx"                   },
            { "senko-san",    "creator of SPT, extremely talented dev, a blast to work with ~ TheSparta" },
            { "leaves",       "Maybe gurls are allowed ~ ssh" }
        };

        #region config values

        // Hidden
        public static ConfigEntry<bool> AcceptedTOS { get; set; }

        // Coop
        public static ConfigEntry<bool> ShowNotifications { get; set; }
        public static ConfigEntry<bool> AutoExtract { get; set; }
        public static ConfigEntry<bool> ShowExtractMessage { get; set; }
        public static ConfigEntry<bool> FasterInventoryScroll { get; set; }
        public static ConfigEntry<int> FasterInventoryScrollSpeed { get; set; }
        public static ConfigEntry<KeyboardShortcut> ExtractKey { get; set; }

        // Coop | Name Plates
        public static ConfigEntry<bool> UseNamePlates { get; set; }
        public static ConfigEntry<bool> HideHealthBar { get; set; }
        public static ConfigEntry<bool> UseHealthNumber { get; set; }
        public static ConfigEntry<bool> UsePlateFactionSide { get; set; }
        public static ConfigEntry<bool> HideNamePlateInOptic { get; set; }
        public static ConfigEntry<bool> DecreaseOpacityNotLookingAt { get; set; }
        public static ConfigEntry<float> NamePlateScale { get; set; }
        public static ConfigEntry<float> OpacityInADS { get; set; }
        public static ConfigEntry<float> MaxDistanceToShow { get; set; }
        public static ConfigEntry<float> MinimumOpacity { get; set; }
        public static ConfigEntry<float> MinimumNamePlateScale { get; set; }

        // Coop | Custom
        public static ConfigEntry<bool> UsePingSystem { get; set; }
        public static ConfigEntry<KeyboardShortcut> PingButton { get; set; }
        public static ConfigEntry<Color> PingColor { get; set; }
        public static ConfigEntry<float> PingSize { get; set; }
        public static ConfigEntry<int> PingTime { get; set; }
        public static ConfigEntry<bool> PlayPingAnimation { get; set; }
        public static ConfigEntry<bool> ShowPingDuringOptics { get; set; }

        // Coop | Debug
        public static ConfigEntry<KeyboardShortcut> FreeCamButton { get; set; }
        public static ConfigEntry<bool> AZERTYMode { get; set; }

        // Performance
        public static ConfigEntry<bool> DynamicAI { get; set; }
        public static ConfigEntry<float> DynamicAIRange { get; set; }
        public static ConfigEntry<DynamicAIRates> DynamicAIRate { get; set; }
        public static ConfigEntry<bool> CullPlayers { get; set; }
        public static ConfigEntry<float> CullingRange { get; set; }

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
        public static ConfigEntry<int> ConnectionTimeout { get; set; }

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
        public bool AllowItemSending;
        public bool ForceSaveOnDeath;
        #endregion

        protected void Awake()
        {
            Instance = this;
            LogDependencyErrors();

            SetupConfig();

            new FikaVersionLabel_Patch().Enable();
            new DisableReadyButton_Patch().Enable();
            new DisableInsuranceReadyButton_Patch().Enable();
            new DisableMatchSettingsReadyButton_Patch().Enable();
            new TarkovApplication_LocalGameCreator_Patch().Enable();
            new DeathFade_Patch().Enable();
            new NonWaveSpawnScenario_Patch().Enable();
            new WaveSpawnScenario_Patch().Enable();
            new WeatherNode_Patch().Enable();
            new EnvironmentUIRoot_Patch().Enable();
            new MatchmakerAcceptScreen_Awake_Patch().Enable();
            new MatchmakerAcceptScreen_Show_Patch().Enable();
            new Minefield_method_2_Patch().Enable();
            new BotCacher_Patch().Enable();
            new InventoryScroll_Patch().Enable();
            new AbstractGame_InRaid_Patch().Enable();
#if GOLDMASTER
            new TOS_Patch().Enable();
#endif

            DisableSPTPatches();
            EnableOverridePatches();

            string fikaVersion = Assembly.GetAssembly(typeof(FikaPlugin)).GetName().Version.ToString();

            Logger.LogInfo($"Fika is loaded! Running version: " + fikaVersion);

            BundleLoaderPlugin = new();
            BundleLoaderPlugin.Create();

            FikaAirdropUtil.GetConfigFromServer();
            GetClientConfig();
            BotSettingsRepoClass.Init();

            if (AllowItemSending)
            {
                new ItemContext_Patch().Enable();
            }

            BotDifficulties = FikaRequestHandler.GetBotDifficulties();
            ConsoleScreen.Processor.RegisterCommandGroup<FikaCommands>();

            StartCoroutine(RunModHandler());
        }

        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Coroutine to ensure all mods are loaded by waiting 5 seconds
        /// </summary>
        /// <returns></returns>
        private IEnumerator RunModHandler()
        {
            yield return new WaitForSeconds(5);
            ModHandler.Run();
        }

        private void GetClientConfig()
        {
            ClientConfigModel clientConfig = FikaRequestHandler.GetClientConfig();

            UseBTR = clientConfig.UseBTR;
            FriendlyFire = clientConfig.FriendlyFire;
            DynamicVExfils = clientConfig.DynamicVExfils;
            AllowFreeCam = clientConfig.AllowFreeCam;
            AllowItemSending = clientConfig.AllowItemSending;
            ForceSaveOnDeath = clientConfig.ForceSaveOnDeath;

            clientConfig.ToString();
        }

        private void SetupConfig()
        {
            // Hidden

            AcceptedTOS = Config.Bind("Hidden", "Accepted TOS", false, new ConfigDescription("Has accepted TOS", tags: new ConfigurationManagerAttributes() { Browsable = false }));

            // Coop

            ShowNotifications = Instance.Config.Bind("Coop", "Show Feed", true, new ConfigDescription("Enable custom notifications when a player dies, extracts, kills a boss, etc.", tags: new ConfigurationManagerAttributes() { Order = 6 }));

            AutoExtract = Config.Bind("Coop", "Auto Extract", false, new ConfigDescription("Automatically extracts after the extraction countdown. As a host, this will only work if there are no clients connected.", tags: new ConfigurationManagerAttributes() { Order = 5 }));

            ShowExtractMessage = Config.Bind("Coop", "Show Extract Message", true, new ConfigDescription("Whether to show the extract message after dying/extracting.", tags: new ConfigurationManagerAttributes() { Order = 4 }));

            FasterInventoryScroll = Config.Bind("Coop", "Faster Inventory Scroll", false, new ConfigDescription("Toggle to increase the inventory scroll speed", tags: new ConfigurationManagerAttributes() { Order = 3 }));

            FasterInventoryScrollSpeed = Config.Bind("Coop", "Faster Inventory Scroll Speed", 63, new ConfigDescription("The speed at which the inventory scrolls at. Default is 63.", new AcceptableValueRange<int>(63, 500), new ConfigurationManagerAttributes() { Order = 2 }));

            ExtractKey = Config.Bind("Coop", "Extract Key", new KeyboardShortcut(KeyCode.F8), new ConfigDescription("The key used to extract from the raid.", tags: new ConfigurationManagerAttributes() { Order = 1 }));

            // Coop | Name Plates

            UseNamePlates = Config.Bind("Coop | Name Plates", "Show Player Name Plates", false, new ConfigDescription("Toggle Health-Bars & Names.", tags: new ConfigurationManagerAttributes() { Order = 10 }));

            HideHealthBar = Config.Bind("Coop | Name Plates", "Hide Health Bar", false, new ConfigDescription("Completely hides the health bar.", tags: new ConfigurationManagerAttributes() { Order = 9 }));

            UseHealthNumber = Config.Bind("Coop | Name Plates", "Show HP% instead of bar", false, new ConfigDescription("Shows health in % amount instead of using the bar.", tags: new ConfigurationManagerAttributes() { Order = 8 }));

            UsePlateFactionSide = Config.Bind("Coop | Name Plates", "Show Player Faction Icon", true, new ConfigDescription("Shows the player faction icon next to the HP bar.", tags: new ConfigurationManagerAttributes() { Order = 7 }));

            HideNamePlateInOptic = Config.Bind("Coop | Name Plates", "Hide Name Plate in Optic", true, new ConfigDescription("Hides the name plate when viewing through PiP scopes since it's kinda janky.", tags: new ConfigurationManagerAttributes() { Order = 6 }));

            DecreaseOpacityNotLookingAt = Config.Bind("Coop | Name Plates", "Decrease Opacity In Peripheral", true, new ConfigDescription("Decreases the opacity of the name plates when not looking at a player.", tags: new ConfigurationManagerAttributes() { Order = 5 }));

            NamePlateScale = Config.Bind("Coop | Name Plates", "Name Plate Scale", 0.22f, new ConfigDescription("Size of the name plates", new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes() { Order = 4 }));

            OpacityInADS = Config.Bind("Coop | Name Plates", "Opacity in ADS", 0.75f, new ConfigDescription("The opacity of the name plates when aiming down sights.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes() { Order = 3 }));

            MaxDistanceToShow = Config.Bind("Coop | Name Plates", "Max Distance to Show", 500f, new ConfigDescription("The maximum distance at which name plates will become invisible, starts to fade at half the input value.", new AcceptableValueRange<float>(10f, 1000f), new ConfigurationManagerAttributes() { Order = 2 }));

            MinimumOpacity = Config.Bind("Coop | Name Plates", "Minimum Opacity", 0.1f, new ConfigDescription("The minimum opacity of the name plates.", new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes() { Order = 1 }));

            MinimumNamePlateScale = Config.Bind("Coop | Name Plates", "Minimum Name Plate Scale", 0.01f, new ConfigDescription("The minimum scale of the name plates.", new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes() { Order = 0 }));

            // Coop | Custom

            UsePingSystem = Config.Bind("Coop | Custom", "Ping System", false, new ConfigDescription("Toggle Ping System. If enabled you can receive and send pings by pressing the ping key.", tags: new ConfigurationManagerAttributes() { Order = 7 }));

            PingButton = Config.Bind("Coop | Custom", "Ping Button", new KeyboardShortcut(KeyCode.U), new ConfigDescription("Button used to send pings.", tags: new ConfigurationManagerAttributes() { Order = 6 }));

            PingColor = Config.Bind("Coop | Custom", "Ping Color", Color.white, new ConfigDescription("The color of your pings when displayed for other players.", tags: new ConfigurationManagerAttributes() { Order = 5 }));

            PingSize = Config.Bind("Coop | Custom", "Ping Size", 1f, new ConfigDescription("The multiplier of the ping size.", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes() { Order = 4 }));

            PingTime = Config.Bind("Coop | Custom", "Ping Time", 3, new ConfigDescription("How long pings should be displayed.", new AcceptableValueRange<int>(2, 10), new ConfigurationManagerAttributes() { Order = 3 }));

            PlayPingAnimation = Config.Bind("Coop | Custom", "Play Ping Animation", false, new ConfigDescription("Plays the pointing animation automatically when pinging. Can interfere with gameplay.", tags: new ConfigurationManagerAttributes() { Order = 2 }));

            ShowPingDuringOptics = Config.Bind("Coop | Custom", "Show Ping During Optics", false, new ConfigDescription("If pings should be displayed while aiming down an optics scope.", tags: new ConfigurationManagerAttributes() { Order = 1 }));

            // Coop | Debug

            FreeCamButton = Config.Bind("Coop | Debug", "Free Camera Button", new KeyboardShortcut(KeyCode.F9), "Button used to toggle free camera.");

            AZERTYMode = Config.Bind("Coop | Debug", "AZERTY Mode", false, "If free camera should use AZERTY keys for input.");

            // Performance

            DynamicAI = Config.Bind("Performance", "Dynamic AI", false, new ConfigDescription("Use the dynamic AI system, disabling AI when they are outside of any player's range.", tags: new ConfigurationManagerAttributes() { Order = 5 }));

            DynamicAIRange = Config.Bind("Performance", "Dynamic AI Range", 100f, new ConfigDescription("The range at which AI will be disabled dynamically.", new AcceptableValueRange<float>(150f, 1000f), new ConfigurationManagerAttributes() { Order = 4 }));

            DynamicAIRate = Config.Bind("Performance", "Dynamic AI Rate", DynamicAIRates.Medium, new ConfigDescription("How often DynamicAI should scan for the range from all players.", tags: new ConfigurationManagerAttributes() { Order = 3 }));

            CullPlayers = Config.Bind("Performance", "Culling System", true, new ConfigDescription("Whether to use the culling system or not. When players are outside of the culling range, their animations will be simplified. This can dramatically improve performance in certain scenarios.", tags: new ConfigurationManagerAttributes() { Order = 2 }));

            CullingRange = Config.Bind("Performance", "Culling Range", 30f, new ConfigDescription("The range at which players should be culled.", new AcceptableValueRange<float>(30f, 150f), new ConfigurationManagerAttributes() { Order = 1 }));

            // Performance | Max Bots

            EnforcedSpawnLimits = Config.Bind("Performance | Max Bots", "Enforced Spawn Limits", false, new ConfigDescription("Enforces spawn limits when spawning bots, making sure to not go over the vanilla limits. This mainly takes affect when using spawn mods or anything that modifies the bot limits. Will not block spawns of special bots like bosses.", tags: new ConfigurationManagerAttributes() { Order = 14 }));

            DespawnFurthest = Config.Bind("Performance | Max Bots", "Despawn Furthest", false, new ConfigDescription("When enforcing spawn limits, should the furthest bot be de-spawned instead of blocking the spawn. This will make for a much more active raid on a lower Max Bots count. Helpful for weaker PCs. Will only despawn pmcs and scavs. If you don't run a dynamic spawn mod, this will however quickly exhaust the spawns on the map, making the raid very dead instead.", tags: new ConfigurationManagerAttributes() { Order = 13 }));

            DespawnMinimumDistance = Config.Bind("Performance | Max Bots", "Despawn Minimum Distance", 200.0f, new ConfigDescription("Don't despawn bots within this distance.", new AcceptableValueRange<float>(50f, 3000f), new ConfigurationManagerAttributes() { Order = 12 }));

            MaxBotsFactory = Config.Bind("Performance | Max Bots", "Max Bots Factory", 0, new ConfigDescription("Max amount of bots that can be active at the same time on Factory. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 11 }));

            MaxBotsCustoms = Config.Bind("Performance | Max Bots", "Max Bots Customs", 0, new ConfigDescription("Max amount of bots that can be active at the same time on Customs. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 10 }));

            MaxBotsInterchange = Config.Bind("Performance | Max Bots", "Max Bots Interchange", 0, new ConfigDescription("Max amount of bots that can be active at the same time on Interchange. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 8 }));

            MaxBotsReserve = Config.Bind("Performance | Max Bots", "Max Bots Reserve", 0, new ConfigDescription("Max amount of bots that can be active at the same time on Reserve. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 7 }));

            MaxBotsWoods = Config.Bind("Performance | Max Bots", "Max Bots Woods", 0, new ConfigDescription("Max amount of bots that can be active at the same time on Woods. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 6 }));

            MaxBotsShoreline = Config.Bind("Performance | Max Bots", "Max Bots Shoreline", 0, new ConfigDescription("Max amount of bots that can be active at the same time on Shoreline. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 5 }));

            MaxBotsStreets = Config.Bind("Performance | Max Bots", "Max Bots Streets", 0, new ConfigDescription("Max amount of bots that can be active at the same time on Streets of Tarkov. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 4 }));

            MaxBotsGroundZero = Config.Bind("Performance | Max Bots", "Max Bots Ground Zero", 0, new ConfigDescription("Max amount of bots that can be active at the same time on Ground Zero. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 3 }));

            MaxBotsLabs = Config.Bind("Performance | Max Bots", "Max Bots Labs", 0, new ConfigDescription("Max amount of bots that can be active at the same time on Labs. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 2 }));

            MaxBotsLighthouse = Config.Bind("Performance | Max Bots", "Max Bots Lighthouse", 0, new ConfigDescription("Max amount of bots that can be active at the same time on Lighthouse. Useful if you have a weaker PC. Set to 0 to use vanilla limits. Cannot be changed during a raid.", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 1 }));

            // Network

            NativeSockets = Config.Bind(section: "Network", "Native Sockets", false, new ConfigDescription("Use NativeSockets for gameplay traffic. This uses direct socket calls for send/receive to drastically increase speed and reduce GC pressure. Only for Windows/Linux and might not always work.", tags: new ConfigurationManagerAttributes() { Order = 7 }));

            ForceIP = Config.Bind("Network", "Force IP", "", new ConfigDescription("Forces the server when hosting to use this IP when broadcasting to the backend instead of automatically trying to fetch it. Leave empty to disable.", tags: new ConfigurationManagerAttributes() { Order = 6 }));

            ForceBindIP = Config.Bind("Network", "Force Bind IP", "", new ConfigDescription("Forces the server when hosting to use this local IP when starting the server. Useful if you are hosting on a VPN.", new AcceptableValueList<string>(GetLocalAddresses()), new ConfigurationManagerAttributes() { Order = 5 }));

            AutoRefreshRate = Config.Bind("Network", "Auto Server Refresh Rate", 10f, new ConfigDescription("Every X seconds the client will ask the server for the list of matches while at the lobby screen.", new AcceptableValueRange<float>(3f, 60f), new ConfigurationManagerAttributes() { Order = 4 }));

            UDPPort = Config.Bind("Network", "UDP Port", 25565, new ConfigDescription("Port to use for UDP gameplay packets.", tags: new ConfigurationManagerAttributes() { Order = 3 }));

            UseUPnP = Config.Bind("Network", "Use UPnP", false, new ConfigDescription("Attempt to open ports using UPnP. Useful if you cannot open ports yourself but the router supports UPnP.", tags: new ConfigurationManagerAttributes() { Order = 2 }));

            ConnectionTimeout = Config.Bind("Network", "Connection Timeout", 15, new ConfigDescription("How long it takes for a connection to be considered dropped if no packets are received.", new AcceptableValueRange<int>(5, 60), new ConfigurationManagerAttributes() { Order = 1 }));

            // Gameplay

            HeadDamageMultiplier = Config.Bind("Gameplay", "Head Damage Multiplier", 1f, new ConfigDescription("X multiplier to damage taken on the head collider. 0.2 = 20%", new AcceptableValueRange<float>(0.2f, 1f), new ConfigurationManagerAttributes() { Order = 4 }));

            ArmpitDamageMultiplier = Config.Bind("Gameplay", "Armpit Damage Multiplier", 1f, new ConfigDescription("X multiplier to damage taken on the armpits collider. 0.2 = 20%", new AcceptableValueRange<float>(0.2f, 1f), new ConfigurationManagerAttributes() { Order = 3 }));

            StomachDamageMultiplier = Config.Bind("Gameplay", "Stomach Damage Multiplier", 1f, new ConfigDescription("X multiplier to damage taken on the stomach collider. 0.2 = 20%", new AcceptableValueRange<float>(0.2f, 1f), new ConfigurationManagerAttributes() { Order = 2 }));

            DisableBotMetabolism = Config.Bind("Gameplay", "Disable Bot Metabolism", false, new ConfigDescription("Disables metabolism on bots, preventing them from dying from loss of energy/hydration during long raids.", tags: new ConfigurationManagerAttributes() { Order = 1 }));
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
            catch (Exception)
            {
                return [.. ips];
            }
        }

        private void DisableSPTPatches()
        {
            // Disable these as they interfere with Fika
            new BotDifficultyPatch().Disable();
            new AirdropPatch().Disable();
            new AirdropFlarePatch().Disable();
            new VersionLabelPatch().Disable();
            new EmptyInfilFixPatch().Disable();
            new OfflineSpawnPointPatch().Disable();
            new BotTemplateLimitPatch().Disable();
            new OfflineRaidSettingsMenuPatch().Disable();
            new AddEnemyToAllGroupsInBotZonePatch().Disable();
            new MaxBotPatch().Disable();
            new LabsKeycardRemovalPatch().Disable(); // We handle this locally instead
            new AmmoUsedCounterPatch().Disable();
            new ArmorDamageCounterPatch().Disable();
            new DogtagPatch().Disable();
            new OfflineSaveProfilePatch().Disable(); // We handle this with our own exit manager

            new BTRInteractionPatch().Disable();
            new BTRExtractPassengersPatch().Disable();
            new BTRPatch().Disable();
        }

        private void EnableOverridePatches()
        {
            new BotDifficultyPatch_Override().Enable();
            new ScavProfileLoad_Override().Enable();
            new MaxBotPatch_Override().Enable();
            new BotTemplateLimitPatch_Override().Enable();
            new OfflineRaidSettingsMenuPatch_Override().Enable();
            new AddEnemyToAllGroupsInBotZonePatch_Override().Enable();
            new FikaAirdropFlare_Patch().Enable();
        }

        private void LogDependencyErrors()
        {
            // Skip if we've already shown the message, or there are no errors
            if (ShownDependencyError || Chainloader.DependencyErrors.Count == 0)
            {
                return;
            }

            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("Errors occurred during plugin loading");
            stringBuilder.AppendLine("-------------------------------------");
            stringBuilder.AppendLine();
            foreach (string error in Chainloader.DependencyErrors)
            {
                stringBuilder.AppendLine(error);
                stringBuilder.AppendLine();
            }
            string errorMessage = stringBuilder.ToString();

            // Show an error in the BepInEx console/log file
            Logger.LogError(errorMessage);

            ShownDependencyError = true;
        }

        public enum DynamicAIRates
        {
            Low,
            Medium,
            High
        }
    }
}