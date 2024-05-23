using Aki.Common.Http;
using Aki.Common.Utils;
using Aki.Custom.Models;
using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using EFT.UI;
using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace Fika.Core.EssentialPatches
{
    /// <summary>
    /// Originally developed by SPT-Aki
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

            string fikaVersion = Assembly.GetAssembly(typeof(FikaVersionLabel_Patch)).GetName().Version.ToString();

            Traverse preloaderUiTraverse = Traverse.Create(MonoBehaviourSingleton<PreloaderUI>.Instance);

            preloaderUiTraverse.Field("_alphaVersionLabel").Property("LocalizationKey").SetValue("{0}");
            preloaderUiTraverse.Field("string_2").SetValue($"Fika {fikaVersion} |");
            Traverse.Create(__result).Field("Major").SetValue($"FIKA BETA {fikaVersion} | {_versionLabel}");
        }
    }
}