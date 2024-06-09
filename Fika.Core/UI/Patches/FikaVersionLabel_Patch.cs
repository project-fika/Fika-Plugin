using System;
using System.Diagnostics;
using System.IO;
using EFT.UI;
using HarmonyLib;
using SPT.Common.Http;
using SPT.Common.Utils;
using SPT.Custom.Models;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System.Linq;
using System.Reflection;

namespace Fika.Core.EssentialPatches
{
    /// <summary>
    /// Originally developed by SPT team
    /// </summary>
    public class FikaVersionLabel_Patch : ModulePatch
    {
        private static string _versionLabel;

        protected override MethodBase GetTargetMethod() => PatchConstants.EftTypes.Single(x => x.GetField("Taxonomy", BindingFlags.Public | BindingFlags.Instance) != null).GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

        [PatchPostfix]
        internal static void PatchPostfix(string major, object __result)
        {
            FikaPlugin.EFTVersionMajor = major;

            if (string.IsNullOrEmpty(_versionLabel))
            {
                string json = RequestHandler.GetJson("/singleplayer/settings/version");
                _versionLabel = Json.Deserialize<VersionResponse>(json).Version;
                Logger.LogInfo($"Server version: {_versionLabel}");
            }

            Traverse preloaderUiTraverse = Traverse.Create(MonoBehaviourSingleton<PreloaderUI>.Instance);

            preloaderUiTraverse.Field("_alphaVersionLabel").Property("LocalizationKey").SetValue("{0}");
            if (FikaPlugin.OfficialVersion.Value)
            {
                string officialVersion = GameVersion();
                preloaderUiTraverse.Field("string_2").SetValue($"{officialVersion} Beta version | PvE ");
                Traverse.Create(__result).Field("Major").SetValue($"{officialVersion} Beta version | {_versionLabel} | PvE ");
            }
            else
            {
                string fikaVersion = Assembly.GetAssembly(typeof(FikaVersionLabel_Patch)).GetName().Version.ToString();
                preloaderUiTraverse.Field("string_2").SetValue($"Fika {fikaVersion} | PvE ");
                Traverse.Create(__result).Field("Major").SetValue($"FIKA BETA {fikaVersion} | {_versionLabel} | PvE ");
            }
        }
        
        /// <summary>
        /// Get exe file version
        /// </summary>
        /// <returns>file version</returns>
        private static string GameVersion()
        {
            var exeVersion = String.Empty;
            var eftPath = string.Empty;
            var eftProcesses = Process.GetProcessesByName("EscapeFromTarkov");
            foreach (var process in eftProcesses)
            {
                Logger.LogDebug("Process path found");
                Logger.LogDebug(process.MainModule.FileName);
                eftPath = process.MainModule.FileName;
                break;
            }

            if (!string.IsNullOrEmpty(eftPath))
            {
                FileInfo fileInfoEft = new(eftPath);
                if (fileInfoEft.Exists)
                {
                    FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(fileInfoEft.FullName);
                    exeVersion = myFileVersionInfo.ProductVersion.Split('-')[0] + "." +
                                 myFileVersionInfo.ProductVersion.Split('-')[1];
                }
            }

            return exeVersion;
        }
    }
}