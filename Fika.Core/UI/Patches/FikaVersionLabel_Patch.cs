using EFT;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPTushonka.Common.Http;
using SPTushonka.Common.Utils;
using SPTushonka.Custom.Models;
using SPTushonka.Reflection.Patching;

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
            _versionLabel = System.Text.Json.JsonSerializer.Deserialize<VersionResponse>(json).Version;
            Logger.LogInfo($"Server version: {_versionLabel}");
        }

        _version = __result;
        _officialVersion = _version.Major;

        UpdateVersionLabel();
    }

    public static void UpdateVersionLabel()
    {
        if (_version == null)
        {
            return;
        }

        var preloader = PreloaderUI.Instance;
        var label = preloader != null ? preloader._alphaVersionLabel : null;
        if (label != null)
        {
            label.LocalizationKey = "{0}";
        }

        if (FikaPlugin.Instance.Settings.OfficialVersion != null && FikaPlugin.Instance.Settings.OfficialVersion.Value)
        {
            if (preloader != null)
            {
                preloader._alphaVersionText = $"{_officialVersion} Beta version";
            }

            _version.Major = _officialVersion;
        }
        else
        {
#if DEBUG
            var text = $"FIKA {FikaPlugin.FikaVersion} (DEBUG) | {_versionLabel}";
#else
            var text = $"FIKA {FikaPlugin.FikaVersion} | {_versionLabel}";
#endif
            if (preloader != null)
            {
                preloader._alphaVersionText = text;
            }

            _version.Major = $"{FikaPlugin.FikaVersion} {_versionLabel}";
        }

        if (preloader != null)
        {
            preloader.RefreshCornerLabel();
        }
    }
}

public class FikaPreloaderVersionLabel_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(PreloaderUI), nameof(PreloaderUI.Awake));
    }

    [PatchPostfix]
    internal static void PatchPostfix()
    {
        FikaVersionLabel_Patch.UpdateVersionLabel();
    }
}
