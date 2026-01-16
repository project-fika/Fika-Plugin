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
    private static Traverse _versionNumberTraverse;
    private static string _officialVersion;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(VersionNumberClass).GetMethod(nameof(VersionNumberClass.Create),
            BindingFlags.Static | BindingFlags.Public);
    }

    [PatchPostfix]
    internal static void PatchPostfix(string major, object __result)
    {
        FikaPlugin.EFTVersionMajor = major;

        if (string.IsNullOrEmpty(_versionLabel))
        {
            var json = RequestHandler.GetJson("/singleplayer/settings/version");
            _versionLabel = Json.Deserialize<VersionResponse>(json).Version;
            Logger.LogInfo($"Server version: {_versionLabel}");
        }

        var preloaderUiTraverse = Traverse.Create(MonoBehaviourSingleton<PreloaderUI>.Instance);

        preloaderUiTraverse.Field("_alphaVersionLabel").Property("LocalizationKey").SetValue("{0}");

        _versionNumberTraverse = Traverse.Create(__result);

        _officialVersion = _versionNumberTraverse.Field<string>("Major").Value;

        UpdateVersionLabel();
    }

    public static void UpdateVersionLabel()
    {
        var preloaderUiTraverse = Traverse.Create(MonoBehaviourSingleton<PreloaderUI>.Instance);
        if (FikaPlugin.Instance.Settings.OfficialVersion != null && FikaPlugin.Instance.Settings.OfficialVersion.Value)
        {
            preloaderUiTraverse.Field("string_2").SetValue($"{_officialVersion} Beta version");
            _versionNumberTraverse.Field("Major").SetValue(_officialVersion);
        }
        else
        {
#if DEBUG
            preloaderUiTraverse.Field("string_2").SetValue($"FIKA {FikaPlugin.FikaVersion} (DEBUG) | {versionLabel}");
#else
            preloaderUiTraverse.Field("string_2").SetValue($"FIKA {FikaPlugin.FikaVersion} | {_versionLabel}");
#endif
            _versionNumberTraverse.Field("Major").SetValue($"{FikaPlugin.FikaVersion} {_versionLabel}");
        }

        // Game mode
        //preloaderUiTraverse.Field("string_4").SetValue("PvE");
        // Update version label
        preloaderUiTraverse.Method("method_6").GetValue();
    }
}