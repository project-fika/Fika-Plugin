using BepInEx;
using BepInEx.Logging;
using Diz.Utils;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.ConsoleCommands;
using Fika.Core.Main.Custom;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Websocket;
#if GOLDMASTER
using Fika.Core.UI; 
#endif
using Fika.Core.UI.Patches;
using SPT.Common.Http;
using SPT.Custom.Patches;
#if RELEASE || GOLDMASTER
using SPT.Custom.Utils;
#endif
using SPT.Reflection.Patching;
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
    public const string FikaVersion = "2.2.2";
    public const string FikaNATPunchMasterServer = "natpunch.project-fika.com";
    public const ushort FikaNATPunchMasterPort = 6790;

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
    public FikaConfig Settings { get; internal set; }

    internal static uint Crc32 { get; set; }
    internal InternalBundleLoader BundleLoaderPlugin { get; private set; }
    internal FikaNotificationManager NotificationManager { get; set; }

#if RELEASE || GOLDMASTER
    private static readonly Version _requiredServerVersion = new("2.2.1");
#endif
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
    public bool AllowNamePlates;
    public bool RandomLabyrinthSpawns;
    public bool PMCFoundInRaid;
    #endregion

    #region natpunch config
    public bool NatPunchServerEnable;
    public string NatPunchServerIP;
    public ushort NatPunchServerPort;
    public int NatPunchServerNatIntroduceAmount;
    #endregion

    protected void Awake()
    {
        Instance = this;
        _patchManager = new(this, true);
        Settings = new(Config);

        GetNatPunchServerConfig();
        EnableModulePatches();
        DisableSPTPatches();

        GetClientConfig();

        var fikaVersion = Assembly.GetAssembly(typeof(FikaPlugin))
            .GetName()
            .Version.ToString();

        Logger.LogInfo($"Fika is loaded! Running version: {fikaVersion}");

        BundleLoaderPlugin = new();

        BotSettingsRepoClass.Init();

        BotDifficulties = FikaRequestHandler.GetBotDifficulties();
        ConsoleScreen.Processor.RegisterCommandGroup<FikaCommands>();

        if (AllowItemSending)
        {
            _patchManager.EnablePatch(new ItemContext_Patch());
        }
    }

    /// <summary>
    /// Run these at start to hopefully ensure that all mods are loaded
    /// </summary>
    protected void Start()
    {
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

    private void EnableModulePatches()
    {
        _patchManager.EnablePatches();
    }

#if RELEASE || GOLDMASTER
    private void VerifyServerVersion()
    {
        var version = FikaRequestHandler.CheckServerVersion().Version;
        var failed = true;
        if (Version.TryParse(version, out var serverVersion))
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
#endif

    /// <summary>
    /// Task that ensure all mods are loaded by waiting 5 seconds
    /// </summary>
    /// <remarks>
    /// The wait is most likely redundant as it runs inside <see cref="Start"/>, however it is kept as last safety check
    /// </remarks>
    private async Task RunChecks()
    {
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

        try
        {
            WanIP = await FikaRequestHandler.GetPublicIP();
        }
        catch (Exception ex)
        {
            Logger.LogError($"RunChecks: {ex.Message}");
        }
    }

    private void GetClientConfig()
    {
        var clientConfig = FikaRequestHandler.GetClientConfig();

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
        AllowNamePlates = clientConfig.AllowNamePlates;
        RandomLabyrinthSpawns = clientConfig.RandomLabyrinthSpawns;
        PMCFoundInRaid = clientConfig.PMCFoundInRaid;

        clientConfig.LogValues();
    }

    private void GetNatPunchServerConfig()
    {
        var natPunchServerConfig = FikaRequestHandler.GetNatPunchServerConfig();

        NatPunchServerEnable = natPunchServerConfig.Enable;

        NatPunchServerIP = RequestHandler.Host.Replace("https://", "")
            .Split(':')[0];

        NatPunchServerPort = (ushort)natPunchServerConfig.Port;

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
        Settings.SetupConfig();
        LocalesLoaded = true;
        AsyncWorker.RunInMainTread(FikaVersionLabel_Patch.UpdateVersionLabel);
    }

    internal string[] GetLocalAddresses()
    {
        List<string> ips = [];
        ips.Add("Disabled");
        ips.Add("0.0.0.0");
        ips.Add("::");

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (!ua.IsDnsEligible)
                    {
                        continue;
                    }

                    var addr = ua.Address;

                    if (IPAddress.IsLoopback(addr))
                    {
                        continue;
                    }

                    if (addr.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (addr.IsIPv6LinkLocal || addr.IsIPv6SiteLocal)
                        {
                            continue;
                        }

                        if (ua.AddressPreferredLifetime == 0)
                        {
                            continue;
                        }
                    }

                    if (addr.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
                    {
                        ips.Add(addr.ToString());
                    }
                }
            }

            LocalIPs = [.. ips.Skip(1)];
            var allIps = string.Join(", ", LocalIPs);
            Logger.LogInfo($"Cached local IPs: {allIps}");
            return [.. ips];
        }
        catch (Exception ex)
        {
            FikaGlobals.LogError("GetLocalAddresses: " + ex.Message);
            return [.. ips];
        }
    }

    private void DisableSPTPatches()
    {
        new VersionLabelPatch().Disable();
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
