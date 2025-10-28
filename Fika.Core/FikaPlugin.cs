﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using Diz.Utils;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.ConsoleCommands;
using Fika.Core.Main.Custom;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using Fika.Core.Networking.Websocket;
#if GOLDMASTER
using Fika.Core.UI; 
#endif
using Fika.Core.UI.Patches;
using SPT.Common.Http;
using SPT.Custom.Patches;
using SPT.Custom.Utils;
using SPT.Reflection.Patching;
using SPT.SinglePlayer.Patches.MainMenu;
using SPT.SinglePlayer.Patches.RaidFix;
using SPT.SinglePlayer.Patches.ScavMode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using static Fika.Core.Networking.IFikaNetworkManager;

namespace Fika.Core;

/// <summary>
/// Fika.Core main plugin. <br/> <br/>
/// Originally by: Paulov <br/>
/// Re-written by: <see langword="Lacyway and the Fika team"/>
/// </summary>
[BepInPlugin("com.fika.core", "Fika.Core", FikaVersion)]
[BepInProcess("EscapeFromTarkov.exe")]
[BepInDependency("com.SPT.custom", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-custom, that way we can disable its patches
[BepInDependency("com.SPT.singleplayer", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-singleplayer, that way we can disable its patches
[BepInDependency("com.SPT.core", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-core, that way we can disable its patches
[BepInDependency("com.SPT.debugging", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-debugging, that way we can disable its patches
public class FikaPlugin : BaseUnityPlugin
{
    public const string FikaVersion = "2.0.4";
    public static FikaPlugin Instance { get; private set; }
    public static string EFTVersionMajor { get; internal set; }
    public ManualLogSource FikaLogger
    {
        get
        {
            return Logger;
        }
    }
    public bool LocalesLoaded { get; internal set; }
    public BotDifficulties BotDifficulties { get; internal set; }
    public FikaModHandler ModHandler = new();
    public string[] LocalIPs { get; internal set; }
    public IPAddress WanIP { get; internal set; }

    internal static uint Crc32 { get; set; }
    internal InternalBundleLoader BundleLoaderPlugin { get; private set; }
    internal FikaNotificationManager NotificationManager { get; set; }

    private static readonly Version _requiredServerVersion = new("1.0.0");
    private PatchManager _patchManager;
    private TarkovApplication _tarkovApp;

    public static HeadlessRequesterWebSocket HeadlessRequesterWebSocket { get; set; }

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
        { "cwx",          "Active and dedicated tester who has contributed a lot of good ideas to Fika. ~ Lacyway"      },
        { "shynd",        "Active contributor and resident helper of Fika ~ Archangel"                                  },
        { "janky",        "It's so, forsooth, alas ~ Lacyway"                                                           }
    };

    public static Dictionary<string, string> DevelopersList = new()
    {
        { "lacyway",      "no one unified the community as much as you ~ Senko-san"                  },
        { "ssh_",         "my little favorite gremlin. ~ Lacyway"                                    },
        { "nexus4880",    "the one who taught me everything I know now. ~ SSH"                       },
        { "thesparta",    "I keep asking him to fix these darn bugs ~ GhostFenixx"                   },
        { "senko-san",    "creator of SPT, extremely talented dev, a blast to work with ~ TheSparta" },
        { "leaves",       "Super talented person who comes up with the coolest ideas ~ Lacyway"      },
        { "archangel",    "The 'tbh' guy :pepeChad: ~ Lacyway"                                       },
        { "trippy",       "One of the chads that made the headless client a reality ~ Archangel"     }
    };

    #region config values

    // Hidden
    public static ConfigEntry<string> LastVersion { get; set; }

    //Advanced
    public static ConfigEntry<bool> OfficialVersion { get; set; }
    public static ConfigEntry<bool> DevMode { get; set; }
    public static ConfigEntry<bool> NoAI { get; set; }
    public static ConfigEntry<bool> NoLoot { get; set; }
    public static ConfigEntry<ELoadPriority> LoadPriority { get; set; }
    public static ConfigEntry<int> MaxBundleLock { get; set; }

    // Coop
    public static ConfigEntry<bool> UseHeadlessIfAvailable { get; set; }
    public static ConfigEntry<bool> ShowNotifications { get; set; }
    public static ConfigEntry<bool> AutoExtract { get; set; }
    public static ConfigEntry<bool> ShowExtractMessage { get; set; }
    public static ConfigEntry<KeyboardShortcut> ExtractKey { get; set; }
    public static ConfigEntry<bool> ShowPlayerList { get; set; }
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
    public static ConfigEntry<bool> ShowEffects { get; set; }
    public static ConfigEntry<bool> UseOcclusion { get; set; }
    public static ConfigEntry<Color> FullHealthColor { get; set; }
    public static ConfigEntry<Color> LowHealthColor { get; set; }
    public static ConfigEntry<Color> NamePlateTextColor { get; set; }

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
    public static ConfigEntry<bool> AllowSpectateBots { get; set; }
    public static ConfigEntry<bool> AZERTYMode { get; set; }
    public static ConfigEntry<bool> DroneMode { get; set; }
    public static ConfigEntry<bool> KeybindOverlay { get; set; }

    // Network
    public static ConfigEntry<string> ForceIP { get; set; }
    public static ConfigEntry<string> ForceBindIP { get; set; }
    public static ConfigEntry<ushort> UDPPort { get; set; }
    public static ConfigEntry<bool> UseUPnP { get; set; }
    public static ConfigEntry<bool> UseNatPunching { get; set; }
    public static ConfigEntry<int> ConnectionTimeout { get; set; }
    public static ConfigEntry<ESendRate> SendRate { get; set; }
    public static ConfigEntry<bool> AllowVOIP { get; set; }

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
    public bool AnyoneCanStartRaid;
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
        _patchManager = new(this, true);

        GetNatPunchServerConfig();
        EnableModulePatches();
        DisableSPTPatches();

        GetClientConfig();

        string fikaVersion = Assembly.GetAssembly(typeof(FikaPlugin)).GetName().Version.ToString();

        Logger.LogInfo($"Fika is loaded! Running version: {fikaVersion}");

        BundleLoaderPlugin = new();

        BotSettingsRepoClass.Init();

        BotDifficulties = FikaRequestHandler.GetBotDifficulties();
        ConsoleScreen.Processor.RegisterCommandGroup<FikaCommands>();

        if (AllowItemSending)
        {
            _patchManager.EnablePatch(new ItemContext_Patch());
        }

        _ = Task.Run(RunChecks);
        _ = Task.Run(GetTarkovApp);
    }

    /// <summary>
    /// Gets the <see cref="TarkovApplication"/>
    /// </summary>
    private async Task GetTarkovApp()
    {
        TarkovApplication app;
        while (!TarkovApplication.Exist(out app))
        {
            await Task.Delay(1000);
        }

        _tarkovApp = app;
    }

    private void SetupConfigEventHandlers()
    {
        OfficialVersion.SettingChanged += OfficialVersion_SettingChanged;
    }

    private void EnableModulePatches()
    {
        _patchManager.EnablePatches();
    }

    private void VerifyServerVersion()
    {
        string version = FikaRequestHandler.CheckServerVersion().Version;
        bool failed = true;
        if (Version.TryParse(version, out Version serverVersion))
        {
            if (serverVersion >= _requiredServerVersion)
            {
                failed = false;
            }
        }

        if (failed)
        {
            FikaLogger.LogError($"Server version check failed. Expected: >{_requiredServerVersion}, received: {serverVersion}");
            AsyncWorker.RunInMainTread(ShowServerCheckFailMessage);
        }
        else
        {
            FikaLogger.LogInfo($"Server version check passed. Expected: >{_requiredServerVersion}, received: {serverVersion}");
        }
    }

    private void ShowServerCheckFailMessage()
    {
        MessageBoxHelper.Show($"Failed to verify server mod version.\nMake sure that the server mod is installed and up-to-date!\nRequired Server Version: {_requiredServerVersion}",
                "FIKA ERROR", MessageBoxHelper.MessageBoxType.OK);
        Application.Quit();
    }

    /// <summary>
    /// Coroutine to ensure all mods are loaded by waiting 5 seconds
    /// </summary>
    private async Task RunChecks()
    {
        try
        {
            WanIP = await FikaRequestHandler.GetPublicIP();
        }
        catch (Exception ex)
        {
            Logger.LogError($"RunChecks: {ex.Message}");
        }

        await Task.Delay(5000);
#if !DEBUG
        VerifyServerVersion();
#endif
        await ModHandler.VerifyMods(_patchManager);

        if (Crc32 == 0)
        {
            Logger.LogError($"RunChecks: {LocaleUtils.UI_MOD_VERIFY_FAIL.Localized()}");
        }

        _patchManager = null;
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
        AnyoneCanStartRaid = clientConfig.AnyoneCanStartRaid;

        clientConfig.LogValues();
    }

    private void GetNatPunchServerConfig()
    {
        NatPunchServerConfigModel natPunchServerConfig = FikaRequestHandler.GetNatPunchServerConfig();

        NatPunchServerEnable = natPunchServerConfig.Enable;
        NatPunchServerIP = RequestHandler.Host.Replace("https://", "").Split(':')[0];
        NatPunchServerPort = natPunchServerConfig.Port;
        NatPunchServerNatIntroduceAmount = natPunchServerConfig.NatIntroduceAmount;

        natPunchServerConfig.LogValues();
    }

    /// <summary>
    /// This is required for the locales to be properly loaded, for some reason they are still unavailable for a few seconds after getting populated
    /// </summary>
    /// <param name="localesTask">The <see cref="Task"/> that populates the locales</param>
    public async void WaitForLocales(Task localesTask)
    {
        Logger.LogInfo("Waiting for locales to be ready...");
        await localesTask;
        if (!FikaBackendUtils.IsHeadless)
        {
            while (LocaleUtils.BEPINEX_H_ADVANCED.Localized() == "F_BepInEx_H_Advanced")
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        Logger.LogInfo("Locales are ready!");
        SetupConfig();
        LocalesLoaded = true;
        AsyncWorker.RunInMainTread(FikaVersionLabel_Patch.UpdateVersionLabel);
    }

    private string CleanConfigString(string header)
    {
        string original = string.Copy(header);
        bool foundForbidden = false;
        char[] forbiddenChars = ['\n', '\t', '\\', '\"', '\'', '[', ']'];
        foreach (char character in forbiddenChars)
        {
            if (header.Contains(character))
            {
                FikaLogger.LogWarning($"Header '{original}' contains an illegal character: {character}\nReport this to the developers!");
                header = header.Replace(character, char.MinValue);
                foundForbidden = true;
            }
        }

        if (foundForbidden)
        {
            FikaLogger.LogWarning($"Header '{original}' was changed to '{header}'");
        }
        return header;
    }

    private ConfigEntry<T> SetupSetting<T>(string section, string key, T defValue, ConfigDescription configDescription, string fallback, ref bool failed, List<string> error)
    {
        try
        {
            return Config.Bind(section, key, defValue, configDescription);
        }
        catch (Exception ex)
        {
            FikaLogger.LogError($"Could not set up section {fallback}! Exception:\n{ex.Message}");
            failed = true;
            error.Add(fallback);

            return Config.Bind(section, fallback, defValue, configDescription);
        }
    }

    private void SetupConfig()
    {
        bool failed = false;
        List<string> headers = [];

        // Hidden

        LastVersion = Config.Bind("Hidden", "Last Version", "0",
            new ConfigDescription("Last loaded version of Fika", tags: new ConfigurationManagerAttributes() { Browsable = false }));

#if GOLDMASTER
        if (LastVersion.Value != FikaVersion)
        {
            Singleton<PreloaderUI>.Instance.ShowFikaMessage("FIKA", LocaleUtils.UI_TOS_LONG.Localized(), ErrorScreen.EButtonType.QuitButton, 15f,
                null, () =>
                {
                    LastVersion.Value = FikaVersion;
                });
        }
#endif

        // Advanced

        string advancedHeader = LocaleUtils.BEPINEX_H_ADVANCED.Localized();
        const string advancedDefaultHeader = "Advanced";

        OfficialVersion = SetupSetting(advancedDefaultHeader, "Show Official Version", false,
                new ConfigDescription(LocaleUtils.BEPINEX_OFFICIAL_VERSION_D.Localized(), tags: new ConfigurationManagerAttributes()
                {
                    IsAdvanced = true,
                    Category = advancedHeader,
                    DispName = LocaleUtils.BEPINEX_OFFICIAL_VERSION_T.Localized(),
                    Order = 6
                }),
                "Official Version", ref failed, headers);

        DevMode = SetupSetting(advancedDefaultHeader, "Developer Mode", false,
            new ConfigDescription("Enables developer features", tags: new ConfigurationManagerAttributes()
            {
                IsAdvanced = true,
                Category = advancedHeader,
                DispName = "Developer Mode",
                Order = 5
            }), "Developer Mode", ref failed, headers);

        NoAI = SetupSetting(advancedDefaultHeader, "No AI", false,
            new ConfigDescription("Stops AI from spawning", tags: new ConfigurationManagerAttributes()
            {
                IsAdvanced = true,
                Category = advancedHeader,
                DispName = "No AI",
                Order = 4
            }), "No AI", ref failed, headers);

        NoLoot = SetupSetting(advancedDefaultHeader, "No Loot", false,
            new ConfigDescription("Stops loot from spawning\nSpeeds up loading for debugging", tags: new ConfigurationManagerAttributes()
            {
                IsAdvanced = true,
                Category = advancedHeader,
                DispName = "No Loot",
                Order = 3
            }), "No Loot", ref failed, headers);

        LoadPriority = SetupSetting(advancedDefaultHeader, "Player Load Priority", ELoadPriority.Low,
            new ConfigDescription("What priority loading other players (and AI as a client) uses.\nMight not have a huge effect.", tags: new ConfigurationManagerAttributes()
            {
                IsAdvanced = true,
                Category = advancedHeader,
                DispName = "Player Load Priority",
                Order = 2
            }), "Player Load Priority", ref failed, headers);

        MaxBundleLock = SetupSetting(advancedDefaultHeader, "Max Bundle Lock", 5,
            new ConfigDescription("Max amount of bundles loading in parallel.\n" +
            "Increase if you take a long time to load bots as a client.\n\n" +
            "Default game value is 1 but has been increased to remedy the base game issue where bundles load too slow",
            new AcceptableValueRange<int>(1, 10), new ConfigurationManagerAttributes()
            {
                IsAdvanced = true,
                Category = advancedHeader,
                DispName = "Max Bundle Lock",
                Order = 1
            }), "Max Bundle Lock", ref failed, headers);

        // Coop

        string coopHeader = CleanConfigString(LocaleUtils.BEPINEX_H_COOP.Localized());
        const string coopDefaultHeader = "Coop";

        UseHeadlessIfAvailable = SetupSetting(coopDefaultHeader, "Auto Use Headless", false,
            new ConfigDescription(LocaleUtils.BEPINEX_USE_HEADLESS_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopHeader,
                DispName = LocaleUtils.BEPINEX_USE_HEADLESS_T.Localized(),
                Order = 9
            }), "Auto Use Headless", ref failed, headers);

        ShowNotifications = SetupSetting(coopDefaultHeader, "Show Feed", true,
            new ConfigDescription(LocaleUtils.BEPINEX_SHOW_FEED_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopHeader,
                DispName = LocaleUtils.BEPINEX_SHOW_FEED_T.Localized(),
                Order = 8
            }),
            "Show Feed", ref failed, headers);

        AutoExtract = SetupSetting(coopDefaultHeader, "Auto Extract", false,
            new ConfigDescription(LocaleUtils.BEPINEX_AUTO_EXTRACT_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopHeader,
                DispName = LocaleUtils.BEPINEX_AUTO_EXTRACT_T.Localized(),
                Order = 7
            }),
            "Auto Extract", ref failed, headers);

        ShowExtractMessage = SetupSetting(coopDefaultHeader, "Show Extract Message", true,
            new ConfigDescription(LocaleUtils.BEPINEX_SHOW_EXTRACT_MESSAGE_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopHeader,
                DispName = LocaleUtils.BEPINEX_SHOW_EXTRACT_MESSAGE_T.Localized(),
                Order = 6
            }),
            "Show Extract Message", ref failed, headers);

        ExtractKey = SetupSetting(coopDefaultHeader, "Extract Key", new KeyboardShortcut(KeyCode.F8),
            new ConfigDescription(LocaleUtils.BEPINEX_EXTRACT_KEY_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopHeader,
                DispName = LocaleUtils.BEPINEX_EXTRACT_KEY_T.Localized(),
                Order = 5
            }),
            "Extract Key", ref failed, headers);

        ShowPlayerList = SetupSetting(coopDefaultHeader, "Show In-Game Player List", true,
            new ConfigDescription(LocaleUtils.BEPINEX_SHOW_EXTRACT_MESSAGE_D.Localized(), tags: new ConfigurationManagerAttributes() // TODO
            {
                Category = coopHeader,
                /* DispName = LocaleUtils.BEPINEX_SHOW_EXTRACT_MESSAGE_T.Localized(), */ // TODO
                DispName = "Show Player List",
                Order = 4
            }),
            "Show In-Game Player List", ref failed, headers);

        EnableChat = SetupSetting(coopDefaultHeader, "Enable Chat", false,
            new ConfigDescription(LocaleUtils.BEPINEX_ENABLE_CHAT_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopHeader,
                DispName = LocaleUtils.BEPINEX_ENABLE_CHAT_T.Localized(),
                Order = 3
            }),
            "Enable Chat", ref failed, headers);

        ChatKey = SetupSetting(coopDefaultHeader, "Chat Key", new KeyboardShortcut(KeyCode.RightControl),
            new ConfigDescription(LocaleUtils.BEPINEX_CHAT_KEY_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopHeader,
                DispName = LocaleUtils.BEPINEX_CHAT_KEY_T.Localized(),
                Order = 2
            }),
            "Chat Key", ref failed, headers);

        EnableOnlinePlayers = SetupSetting(coopDefaultHeader, "Enable Online Players", true,
            new ConfigDescription(LocaleUtils.BEPINEX_ENABLE_ONLINE_PLAYER_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopHeader,
                DispName = LocaleUtils.BEPINEX_ENABLE_ONLINE_PLAYER_T.Localized(),
                Order = 1
            }),
            "Enable Online Players", ref failed, headers);

        OnlinePlayersScale = SetupSetting(coopDefaultHeader, "Online Players Scale", 1f,
            new ConfigDescription(LocaleUtils.BEPINEX_ONLINE_PLAYERS_SCALE_D.Localized(),
            new AcceptableValueRange<float>(0.5f, 1.5f), new ConfigurationManagerAttributes()
            {
                Category = coopHeader,
                DispName = LocaleUtils.BEPINEX_ONLINE_PLAYERS_SCALE_T.Localized(),
                Order = 0
            }),
            "Online Players Scale", ref failed, headers);

        // Coop | Name Plates

        string coopNameplatesHeader = CleanConfigString(LocaleUtils.BEPINEX_H_COOP_NAME_PLATES.Localized());
        const string coopDefaultNamePlatesHeader = "Coop | Name Plates";

        UseNamePlates = SetupSetting(coopDefaultNamePlatesHeader, "Show Player Name Plates", true,
            new ConfigDescription(LocaleUtils.BEPINEX_USE_NAME_PLATES_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_USE_NAME_PLATES_T.Localized(),
                Order = 16
            }),
            "Show Player Name Plates", ref failed, headers);

        HideHealthBar = SetupSetting(coopDefaultNamePlatesHeader, "Hide Health Bar", false,
            new ConfigDescription(LocaleUtils.BEPINEX_HIDE_HEALTH_BAR_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_HIDE_HEALTH_BAR_T.Localized(),
                Order = 15
            }),
            "Hide Health Bar", ref failed, headers);

        UseHealthNumber = SetupSetting(coopDefaultNamePlatesHeader, "Show HP% instead of bar", false,
            new ConfigDescription(LocaleUtils.BEPINEX_USE_PERCENT_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_USE_PERCENT_T.Localized(),
                Order = 14
            }),
            "Show HP% instead of bar", ref failed, headers);

        ShowEffects = SetupSetting(coopDefaultNamePlatesHeader, "Show Effects", true,
            new ConfigDescription(LocaleUtils.BEPINEX_SHOW_EFFECTS_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_SHOW_EFFECTS_T.Localized(),
                Order = 13
            }),
            "Show Effects", ref failed, headers);

        UsePlateFactionSide = SetupSetting(coopDefaultNamePlatesHeader, "Show Player Faction Icon", true,
            new ConfigDescription(LocaleUtils.BEPINEX_SHOW_FACTION_ICON_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_SHOW_FACTION_ICON_T.Localized(),
                Order = 12
            }),
            "Show Player Faction Icon", ref failed, headers);

        HideNamePlateInOptic = SetupSetting(coopDefaultNamePlatesHeader, "Hide Name Plate in Optic", true,
            new ConfigDescription(LocaleUtils.BEPINEX_HIDE_IN_OPTIC_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_HIDE_IN_OPTIC_T.Localized(),
                Order = 11
            }),
            "Hide Name Plate in Optic", ref failed, headers);

        NamePlateUseOpticZoom = SetupSetting(coopDefaultNamePlatesHeader, "Name Plates Use Optic Zoom", true,
            new ConfigDescription(LocaleUtils.BEPINEX_OPTIC_USE_ZOOM_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_OPTIC_USE_ZOOM_T.Localized(),
                Order = 10,
                IsAdvanced = true
            }),
            "Name Plates Use Optic Zoom", ref failed, headers);

        DecreaseOpacityNotLookingAt = SetupSetting(coopDefaultNamePlatesHeader, "Decrease Opacity In Peripheral", true,
            new ConfigDescription(LocaleUtils.BEPINEX_DEC_OPAC_PERI_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_DEC_OPAC_PERI_T.Localized(),
                Order = 9
            }),
            "Decrease Opacity In Peripheral", ref failed, headers);

        NamePlateScale = SetupSetting(coopDefaultNamePlatesHeader, "Name Plate Scale", 1f,
            new ConfigDescription(LocaleUtils.BEPINEX_NAME_PLATE_SCALE_D.Localized(),
            new AcceptableValueRange<float>(0.5f, 1.5f), new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_NAME_PLATE_SCALE_T.Localized(),
                Order = 8
            }),
            "Name Plate Scale", ref failed, headers);

        OpacityInADS = SetupSetting(coopDefaultNamePlatesHeader, "Opacity in ADS", 0.75f,
            new ConfigDescription(LocaleUtils.BEPINEX_ADS_OPAC_D.Localized(),
            new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_ADS_OPAC_T.Localized(),
                Order = 7
            }),
            "Opacity in ADS", ref failed, headers);

        MaxDistanceToShow = SetupSetting(coopDefaultNamePlatesHeader, "Max Distance to Show", 500f,
            new ConfigDescription(LocaleUtils.BEPINEX_MAX_DISTANCE_D.Localized(),
            new AcceptableValueRange<float>(10f, 1000f), new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_MAX_DISTANCE_T.Localized(),
                Order = 6
            }),
            "Max Distance to Show", ref failed, headers);

        MinimumOpacity = SetupSetting(coopDefaultNamePlatesHeader, "Minimum Opacity", 0.1f,
            new ConfigDescription(LocaleUtils.BEPINEX_MIN_OPAC_D.Localized(),
            new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_MIN_OPAC_T.Localized(),
                Order = 5
            }),
            "Minimum Opacity", ref failed, headers);

        UseOcclusion = SetupSetting(coopDefaultNamePlatesHeader, "Use Occlusion", false,
            new ConfigDescription(LocaleUtils.BEPINEX_USE_OCCLUSION_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_USE_OCCLUSION_T.Localized(),
                Order = 3
            }),
            "Use Occlusion", ref failed, headers);

        FullHealthColor = SetupSetting(coopDefaultNamePlatesHeader, "Full Health Color", Color.green,
            new ConfigDescription(LocaleUtils.BEPINEX_HEALTHCOLOR_FULL_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_HEALTHCOLOR_FULL_T.Localized(),
                Order = 2
            }),
            "Full Health Color", ref failed, headers);

        LowHealthColor = SetupSetting(coopDefaultNamePlatesHeader, "Low Health Color", Color.red,
            new ConfigDescription(LocaleUtils.BEPINEX_HEALTHCOLOR_LOW_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_HEALTHCOLOR_LOW_T.Localized(),
                Order = 1
            }),
            "Low Health Color", ref failed, headers);

        NamePlateTextColor = SetupSetting(coopDefaultNamePlatesHeader, "Name Plate Text Color", Color.white,
            new ConfigDescription(LocaleUtils.BEPINEX_NAMEPLATECOLOR_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopNameplatesHeader,
                DispName = LocaleUtils.BEPINEX_NAMEPLATECOLOR_T.Localized(),
                Order = 0
            }),
            "Name Plate Text Color", ref failed, headers);

        // Coop | Quest Sharing

        string coopQuestSharingHeader = CleanConfigString(LocaleUtils.BEPINEX_H_COOP_QUEST_SHARING.Localized());
        const string coopDefaultQuestSharingHeader = "Coop | Quest Sharing";
        bool questSharingEnabled = SharedQuestProgression;
        string disabledMessage = LocaleUtils.UI_DISABLED_BY_HOST.Localized();

        QuestTypesToShareAndReceive = SetupSetting(coopDefaultQuestSharingHeader, "Quest Types", EQuestSharingTypes.All,
            new ConfigDescription(questSharingEnabled ? LocaleUtils.BEPINEX_QUEST_TYPES_D.Localized() : disabledMessage, tags: new ConfigurationManagerAttributes()
            {
                Category = coopQuestSharingHeader,
                DispName = LocaleUtils.BEPINEX_QUEST_TYPES_T.Localized(),
                Order = 4,
                ReadOnly = !questSharingEnabled
            }),
            "Quest Types", ref failed, headers);

        QuestSharingNotifications = SetupSetting(coopDefaultQuestSharingHeader, "Show Notifications", true,
            new ConfigDescription(questSharingEnabled ? LocaleUtils.BEPINEX_QS_NOTIFICATIONS_D.Localized() : disabledMessage, tags: new ConfigurationManagerAttributes()
            {
                Category = coopQuestSharingHeader,
                DispName = LocaleUtils.BEPINEX_QS_NOTIFICATIONS_T.Localized(),
                Order = 3,
                ReadOnly = !questSharingEnabled
            }),
            "Show Notifications", ref failed, headers);

        EasyKillConditions = SetupSetting(coopDefaultQuestSharingHeader, "Easy Kill Conditions", false,
            new ConfigDescription(questSharingEnabled ? LocaleUtils.BEPINEX_EASY_KILL_CONDITIONS_D.Localized() : disabledMessage, tags: new ConfigurationManagerAttributes()
            {
                Category = coopQuestSharingHeader,
                DispName = LocaleUtils.BEPINEX_EASY_KILL_CONDITIONS_T.Localized(),
                Order = 2,
                ReadOnly = !questSharingEnabled
            }),
            "Easy Kill Conditions", ref failed, headers);

        SharedKillExperience = SetupSetting(coopDefaultQuestSharingHeader, "Shared Kill Experience", false,
            new ConfigDescription(questSharingEnabled ? LocaleUtils.BEPINEX_SHARED_KILL_XP_D.Localized() : disabledMessage, tags: new ConfigurationManagerAttributes()
            {
                Category = coopQuestSharingHeader,
                DispName = LocaleUtils.BEPINEX_SHARED_KILL_XP_T.Localized(),
                Order = 1,
                ReadOnly = !questSharingEnabled
            }),
            "Shared Kill Experience", ref failed, headers);

        SharedBossExperience = SetupSetting(coopDefaultQuestSharingHeader, "Shared Boss Experience", false,
            new ConfigDescription(questSharingEnabled ? LocaleUtils.BEPINEX_SHARED_BOSS_XP_D.Localized() : disabledMessage, tags: new ConfigurationManagerAttributes()
            {
                Category = coopQuestSharingHeader,
                DispName = LocaleUtils.BEPINEX_SHARED_BOSS_XP_T.Localized(),
                Order = 0,
                ReadOnly = !questSharingEnabled
            }),
            "Shared Boss Experience", ref failed, headers);

        // Coop | Pinging

        string coopPingingHeader = CleanConfigString(LocaleUtils.BEPINEX_H_COOP_PINGING.Localized());
        const string coopDefaultPingingHeader = "Coop | Pinging";

        UsePingSystem = SetupSetting(coopDefaultPingingHeader, "Ping System", true,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_SYSTEM_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_SYSTEM_T.Localized(),
                Order = 11
            }),
            "Ping System", ref failed, headers);

        PingButton = SetupSetting(coopDefaultPingingHeader, "Ping Button", new KeyboardShortcut(KeyCode.Semicolon),
            new ConfigDescription(LocaleUtils.BEPINEX_PING_BUTTON_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_BUTTON_T.Localized(),
                Order = 10
            }),
            "Ping Button", ref failed, headers);

        PingColor = SetupSetting(coopDefaultPingingHeader, "Ping Color", Color.white,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_COLOR_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_COLOR_T.Localized(),
                Order = 9
            }),
            "Ping Color", ref failed, headers);

        PingSize = SetupSetting(coopDefaultPingingHeader, "Ping Size", 1f,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_SIZE_D.Localized(),
            new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_SIZE_T.Localized(),
                Order = 8
            }),
            "Ping Size", ref failed, headers);

        PingTime = SetupSetting(coopDefaultPingingHeader, "Ping Time", 3,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_TIME_D.Localized(),
            new AcceptableValueRange<int>(2, 10), new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_TIME_T.Localized(),
                Order = 7
            }),
            "Ping Time", ref failed, headers);

        PlayPingAnimation = SetupSetting(coopDefaultPingingHeader, "Play Ping Animation", false,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_ANIMATION_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_ANIMATION_T.Localized(),
                Order = 6
            }),
            "Play Ping Animation", ref failed, headers);

        ShowPingDuringOptics = SetupSetting(coopDefaultPingingHeader, "Show Ping During Optics", false,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_OPTICS_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_OPTICS_T.Localized(),
                Order = 5
            }),
            "Show Ping During Optics", ref failed, headers);

        PingUseOpticZoom = SetupSetting(coopDefaultPingingHeader, "Ping Use Optic Zoom", true,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_OPTIC_ZOOM_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_OPTIC_ZOOM_T.Localized(),
                Order = 4,
                IsAdvanced = true
            }),
            "Ping Use Optic Zoom", ref failed, headers);

        PingScaleWithDistance = SetupSetting(coopDefaultPingingHeader, "Ping Scale With Distance", true,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_SCALE_DISTANCE_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_SCALE_DISTANCE_T.Localized(),
                Order = 3,
                IsAdvanced = true
            }),
            "Ping Scale With Distance", ref failed, headers);

        PingMinimumOpacity = SetupSetting(coopDefaultPingingHeader, "Ping Minimum Opacity", 0.05f,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_MIN_OPAC_D.Localized(),
            new AcceptableValueRange<float>(0f, 0.5f), new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_MIN_OPAC_T.Localized(),
                Order = 2,
                IsAdvanced = true
            }),
            "Ping Minimum Opacity", ref failed, headers);

        ShowPingRange = SetupSetting(coopDefaultPingingHeader, "Show Ping Range", false,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_RANGE_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_RANGE_T.Localized(),
                Order = 1
            }),
            "Show Ping Range", ref failed, headers);

        PingSound = SetupSetting(coopDefaultPingingHeader, "Ping Sound", EPingSound.SubQuestComplete,
            new ConfigDescription(LocaleUtils.BEPINEX_PING_SOUND_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopPingingHeader,
                DispName = LocaleUtils.BEPINEX_PING_SOUND_T.Localized(),
                Order = 0
            }),
            "Ping Sound", ref failed, headers);

        // Coop | Debug

        string coopDebugHeader = CleanConfigString(LocaleUtils.BEPINEX_H_COOP_DEBUG.Localized());
        const string coopDefaultDebugHeader = "Coop | Debug";

        FreeCamButton = SetupSetting(coopDefaultDebugHeader, "Free Camera Button", new KeyboardShortcut(KeyCode.F9),
            new ConfigDescription(CleanConfigString(LocaleUtils.BEPINEX_FREE_CAM_BUTTON_D.Localized()), tags: new ConfigurationManagerAttributes()
            {
                Category = coopDebugHeader,
                DispName = LocaleUtils.BEPINEX_FREE_CAM_BUTTON_T.Localized(),
                Order = 4
            }),
            "Free Camera Button", ref failed, headers);

        AllowSpectateBots = SetupSetting(coopDefaultDebugHeader, "Allow Spectating Bots", true,
            new ConfigDescription(CleanConfigString(LocaleUtils.BEPINEX_SPECTATE_BOTS_D.Localized()), tags: new ConfigurationManagerAttributes()
            {
                Category = coopDebugHeader,
                DispName = LocaleUtils.BEPINEX_SPECTATE_BOTS_T.Localized(),
                Order = 3
            }),
            "Allow Spectating Bots", ref failed, headers);

        AZERTYMode = SetupSetting(coopDefaultDebugHeader, "AZERTY Mode", false,
            new ConfigDescription(CleanConfigString(LocaleUtils.BEPINEX_AZERTY_MODE_D.Localized()), tags: new ConfigurationManagerAttributes()
            {
                Category = coopDebugHeader,
                DispName = LocaleUtils.BEPINEX_AZERTY_MODE_T.Localized(),
                Order = 2
            }),
            "AZERTY Mode", ref failed, headers);

        DroneMode = SetupSetting(coopDefaultDebugHeader, "Drone Mode", false,
            new ConfigDescription(LocaleUtils.BEPINEX_DRONE_MODE_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopDebugHeader,
                DispName = LocaleUtils.BEPINEX_DRONE_MODE_T.Localized(),
                Order = 1
            }),
            "Drone Mode", ref failed, headers);

        KeybindOverlay = SetupSetting(coopDefaultDebugHeader, "Keybind Overlay", true,
            new ConfigDescription(LocaleUtils.BEPINEX_KEYBIND_OVERLAY_T.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = coopDebugHeader,
                DispName = LocaleUtils.BEPINEX_KEYBIND_OVERLAY_T.Localized(),
                Order = 0
            }),
            "Keybind Overlay", ref failed, headers);

        // Network

        string networkHeader = CleanConfigString(LocaleUtils.BEPINEX_H_NETWORK.Localized());
        const string networkDefaultHeader = "Network";

        ForceIP = SetupSetting(networkDefaultHeader, "Force IP", "",
            new ConfigDescription(LocaleUtils.BEPINEX_FORCE_IP_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = networkHeader,
                DispName = LocaleUtils.BEPINEX_FORCE_IP_T.Localized(),
                Order = 8
            }),
            "Force IP", ref failed, headers);

        ForceBindIP = SetupSetting(networkDefaultHeader, "Force Bind IP", "0.0.0.0",
            new ConfigDescription(LocaleUtils.BEPINEX_FORCE_BIND_IP_D.Localized(),
            new AcceptableValueList<string>(GetLocalAddresses()), new ConfigurationManagerAttributes()
            {
                Category = networkHeader,
                DispName = LocaleUtils.BEPINEX_FORCE_BIND_IP_T.Localized(),
                Order = 7
            }),
            "Force Bind IP", ref failed, headers);

        UDPPort = SetupSetting(networkDefaultHeader, "UDP Port", (ushort)25565,
            new ConfigDescription(LocaleUtils.BEPINEX_UDP_PORT_D.Localized(), new AcceptableValueRange<ushort>(ushort.MinValue, ushort.MaxValue),
            tags: new ConfigurationManagerAttributes()
            {
                Category = networkHeader,
                DispName = LocaleUtils.BEPINEX_UDP_PORT_T.Localized(),
                Order = 5
            }),
            "UDP Port", ref failed, headers);

        UseUPnP = SetupSetting(networkDefaultHeader, "Use UPnP", false,
            new ConfigDescription(LocaleUtils.BEPINEX_USE_UPNP_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = networkHeader,
                DispName = LocaleUtils.BEPINEX_USE_UPNP_T.Localized(),
                Order = 4,
                IsAdvanced = true
            }),
            "Use UPnP", ref failed, headers);

        UseNatPunching = SetupSetting(networkDefaultHeader, "Use NAT Punching", false,
            new ConfigDescription(LocaleUtils.BEPINEX_USE_NAT_PUNCH_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = networkHeader,
                DispName = LocaleUtils.BEPINEX_USE_NAT_PUNCH_T.Localized(),
                Order = 3,
                IsAdvanced = true
            }),
            "Use NAT Punching", ref failed, headers);

        ConnectionTimeout = SetupSetting(networkDefaultHeader, "Connection Timeout", 30,
            new ConfigDescription(LocaleUtils.BEPINEX_CONNECTION_TIMEOUT_D.Localized(),
            new AcceptableValueRange<int>(5, 60), new ConfigurationManagerAttributes()
            {
                Category = networkHeader,
                DispName = LocaleUtils.BEPINEX_CONNECTION_TIMEOUT_T.Localized(),
                Order = 2
            }),
            "Connection Timeout", ref failed, headers);

        SendRate = SetupSetting(networkDefaultHeader, "Send Rate", ESendRate.Medium,
            new ConfigDescription(LocaleUtils.BEPINEX_SEND_RATE_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = networkHeader,
                DispName = LocaleUtils.BEPINEX_SEND_RATE_T.Localized(),
                Order = 1
            }),
            "Send Rate", ref failed, headers);

        AllowVOIP = SetupSetting(networkDefaultHeader, "Allow VOIP", false,
            new ConfigDescription(LocaleUtils.BEPINEX_NET_VOIP_D.Localized(), tags: new ConfigurationManagerAttributes()
            {
                Category = networkHeader,
                DispName = LocaleUtils.BEPINEX_NET_VOIP_T.Localized(),
                Order = 0
            }),
            "Allow VOIP", ref failed, headers);

        // Gameplay

        DisableBotMetabolism = SetupSetting("Gameplay", "Disable Bot Metabolism",
            false, new ConfigDescription(LocaleUtils.BEPINEX_DISABLE_BOT_METABOLISM_D.Localized(),
            tags: new ConfigurationManagerAttributes()
            {
                Category = LocaleUtils.BEPINEX_H_GAMEPLAY.Localized(),
                DispName = LocaleUtils.BEPINEX_DISABLE_BOT_METABOLISM_T.Localized(),
                Order = 1
            }),
            "Disable Bot Metabolism", ref failed, headers);

        if (failed)
        {
            string headerString = string.Join(", ", headers);
            Singleton<PreloaderUI>.Instance.ShowErrorScreen(LocaleUtils.UI_LOCALE_ERROR_HEADER.Localized(),
                string.Format(LocaleUtils.UI_LOCALE_ERROR_DESCRIPTION.Localized(), headerString));
            FikaLogger.LogError("SetupConfig: Headers failed: " + headerString);
        }

        SetupConfigEventHandlers();

        if (ForceBindIP.Value == "Disabled" && FikaBackendUtils.VPNIP != null)
        {
            ForceBindIP.Value = FikaBackendUtils.VPNIP.ToString();
            Logger.LogInfo($"Auto-detected VPN IP: {FikaBackendUtils.VPNIP}, setting as ForceBindIP");
        }
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
                    if (networkInterface.Description.Contains("Radmin VPN") || networkInterface.Description.Contains("ZeroTier"))
                    {
                        FikaBackendUtils.VPNIP = ip.Address;
                    }

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

            LocalIPs = [.. ips.Skip(1)];
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
        /*// Disable these as they interfere with Fika (currently does not work, use below)
        var patches = ModPatchCache.GetActivePatches();
        var targets = new HashSet<string>
        {
            nameof(VersionLabelPatch),
            nameof(AmmoUsedCounterPatch),
            nameof(ArmorDamageCounterPatch),
            nameof(ScavRepAdjustmentPatch),
            nameof(GetProfileAtEndOfRaidPatch),
            nameof(ScavExfilPatch),
            nameof(SendPlayerScavProfileToServerAfterRaidPatch),
            nameof(MatchStartServerLocationPatch),
            nameof(QuestAchievementRewardInRaidPatch)
        };

        for (var i = 0; i < patches.Count; i++)
        {
            var patch = patches[i];
            var name = patch.GetType().Name;
            Logger.LogWarning(name);
            if (targets.Contains(name))
            {
                Logger.LogInfo($"Found {name}, disabling...");
                patch.Disable();
            }
        }*/

        new VersionLabelPatch().Disable();
        new AmmoUsedCounterPatch().Disable();
        new ScavRepAdjustmentPatch().Disable();
        new GetProfileAtEndOfRaidPatch().Disable();
        new ScavExfilPatch().Disable();
        new SendPlayerScavProfileToServerAfterRaidPatch().Disable();
        new MatchStartServerLocationPatch().Disable();
        new QuestAchievementRewardInRaidPatch().Disable();
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

    [Flags]
    public enum EQuestSharingTypes
    {
        None = 0,
        Kills = 1,
        Item = 2,
        Location = 4,
        PlaceBeacon = 8,

        All = Kills | Item | Location | PlaceBeacon
    }

    public enum ELoadPriority
    {
        Low,
        Medium,
        High
    }
}
