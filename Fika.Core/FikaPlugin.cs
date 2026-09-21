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
using SPTushonka.Common.Http;
using SPTushonka.Custom.Patches;
using SPTushonka.SinglePlayer.Patches.Cutscenes;
#if RELEASE || GOLDMASTER
using SPTushonka.Custom.Utils;
#endif
using SPTushonka.Reflection.Patching;
using BepInEx.Unity.IL2CPP;
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
[BepInDependency("com.sptushonka.custom", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-custom, that way we can disable its patches
[BepInDependency("com.sptushonka.singleplayer", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-singleplayer, that way we can disable its patches
[BepInDependency("com.sptushonka.core", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-core, that way we can disable its patches
[BepInDependency("com.sptushonka.debugging", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-debugging, that way we can disable its patches
public sealed class FikaPlugin : BasePlugin
{
    public const string FikaVersion = "3.0.0";
    public const string FikaNATPunchMasterServer = "natpunch.project-fika.com";
    public const ushort FikaNATPunchMasterPort = 6790;

    public static FikaPlugin Instance { get; private set; }
    public static string EFTVersionMajor { get; internal set; }
    public ManualLogSource FikaLogger
    {
        get
        {
            return Log;
        }
    }
    public bool LocalesLoaded { get; internal set; }
    public BotDifficulties BotDifficulties { get; internal set; }
    public FikaModHandler ModHandler = new();
    public string[] LocalIPs { get; internal set; }
    public IPAddress WanIP { get; internal set; }
    public FikaConfig Settings { get; internal set; }
    public GameUI GameUi => MonoBehaviourSingleton<GameUI>.Instance;

    internal static uint Crc32 { get; set; }
    internal InternalBundleLoader BundleLoaderPlugin { get; private set; }
    internal FikaNotificationManager NotificationManager { get; set; }

#if RELEASE || GOLDMASTER
    private static readonly System.Version _requiredServerVersion = new("2.4.0");
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

    #region natpunch config
    public bool NatPunchServerEnable;
    public string NatPunchServerIP;
    public ushort NatPunchServerPort;
    public int NatPunchServerNatIntroduceAmount;
    #endregion

    public override void Load()
    {
        Instance = this;

        // Todo: needed this for some logging, check if I still need it later
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(LogUnhandled);
        TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(LogUnobserved);
        Application.add_quitting(new System.Action(FikaGlobals.OnApplicationQuitting));
        Il2CppInjection.RegisterAll(typeof(FikaPlugin).Assembly, Log);
        _patchManager = new(this, true);
        Settings = new FikaConfig(Config);

        GetNatPunchServerConfig();
        EnableModulePatches();
        DisableSPTPatches();

        GetClientConfig();

        var fikaVersion = Assembly.GetAssembly(typeof(FikaPlugin))
            .GetName()
            .Version.ToString();

        Log.LogInfo($"Fika is loaded! Running version: {fikaVersion}");

        BundleLoaderPlugin = new();

        WildSpawnTypeExtension.Init();

        BotDifficulties = FikaRequestHandler.GetBotDifficulties();

        if (Settings.AllowItemSending)
        {
            _patchManager.EnablePatch(new ItemContext_Patch());
        }

        if (Settings.FastLoad)
        {
            /*_patchManager.EnablePatch(new LoadAmmo_Task_Transpiler());
            _patchManager.EnablePatch(new ItemViewLoadAmmoComponent_Show_Patch());*/
        }

        _ = RunChecks();
        _ = GetTarkovApp();
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
        if (System.Version.TryParse(version, out var serverVersion))
        {
            if (serverVersion >= _requiredServerVersion)
            {
                failed = false;
            }
        }

        if (failed)
        {
            FikaLogger.LogError($"Server version check failed. Expected: >{_requiredServerVersion}, received: {serverVersion}");
            MainThread.Post(ShowServerCheckFailMessage);
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
            Log.LogError($"RunChecks: {LocaleUtils.UI_MOD_VERIFY_FAIL.Localized()}");
        }

        _patchManager = null;

        try
        {
            WanIP = await FikaRequestHandler.GetPublicIP();
        }
        catch (Exception ex)
        {
            Log.LogError($"RunChecks: {ex.Message}");
        }
    }

    private void GetClientConfig()
    {
        Settings.GetClientConfig();
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
        try
        {
            await WaitForLocalesInternal(localesTask);
        }
        catch (Exception ex)
        {
            Log.LogError($"WaitForLocales: {ex}");
        }
    }

    private async Task WaitForLocalesInternal(Task localesTask)
    {
        Log.LogInfo("Waiting for locales to be ready...");
        await localesTask;
        if (!FikaBackendUtils.IsHeadless)
        {
            while (LocaleUtils.BEPINEX_H_ADVANCED.Localized() == "F_BepInEx_H_Advanced")
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        Log.LogInfo("Locales are ready!");
        Settings.SetupConfig();
        LocalesLoaded = true;
        MainThread.Post(FikaVersionLabel_Patch.UpdateVersionLabel);
    }

    private static void LogUnhandled(object sender, UnhandledExceptionEventArgs args)
    {
        FikaGlobals.LogFatal($"Unhandled exception on a background thread: {args.ExceptionObject}");
    }

    private static void LogUnobserved(object sender, UnobservedTaskExceptionEventArgs args)
    {
        FikaGlobals.LogError($"Unobserved task exception: {args.Exception}");
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
            Log.LogInfo($"Cached local IPs: {allIps}");
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
        new FinalMissionDirectorPatches.StartPatch().Disable();
        new FinalMissionDirectorPatches.TickPatch().Disable();
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
