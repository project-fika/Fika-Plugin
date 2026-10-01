using EFT;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Common.Http;
using SPT.Common.Utils;
using SPT.Custom.Models;
using SPT.Reflection.Patching;

namespace Fika.Core.UI.Patches;

/// <summary>
/// Originally developed by SPT team
/// </summary>
public class FikaVersionLabel_Patch : ModulePatch
{
    private static string _versionLabel;
    private static Version _version;
    private static string _officialVersion;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(Version).GetMethod(nameof(Version.Create),
            BindingFlags.Static | BindingFlags.Public);
    }

    [PatchPostfix]
    internal static void PatchPostfix(string major, Version __result)
    {
        FikaPlugin.EFTVersionMajor = major;

        if (string.IsNullOrEmpty(_versionLabel))
        {
            var json = RequestHandler.GetJson("/singleplayer/settings/version");
            _versionLabel = Json.Deserialize<VersionResponse>(json).Version;
            Logger.LogInfo($"Server version: {_versionLabel}");
        }

        if (!MonoBehaviourSingleton<PreloaderUI>.Instantiated) // PreloaderUI is not ready yet
        {
            return;
        }

        var preloaderUI = MonoBehaviourSingleton<PreloaderUI>.Instance;
        preloaderUI._alphaVersionLabel.LocalizationKey = "{0}";

        _version = __result;
        _officialVersion = __result.Major;

        UpdateVersionLabel();
    }

    public static void UpdateVersionLabel()
    {
        var preloaderUI = MonoBehaviourSingleton<PreloaderUI>.Instance;
        if (FikaPlugin.Instance.Settings.OfficialVersion != null && FikaPlugin.Instance.Settings.OfficialVersion.Value)
        {
            preloaderUI.string_2 = $"{_officialVersion} Beta version";
            _version.Major = _officialVersion;
        }
        else
        {
#if DEBUG
            preloaderUI.string_2 = $"FIKA {FikaPlugin.FikaVersion} (DEBUG) | {_versionLabel}";
#else
            preloaderUI.string_2 = $"FIKA {FikaPlugin.FikaVersion} | {_versionLabel}";
#endif
            _version.Major = $"{FikaPlugin.FikaVersion} {_versionLabel}";
        }

        // update version label
        preloaderUI.RefreshCornerLabel();
    }
}