using EFT.UI;
using HarmonyLib;
using SPT.Common.Http;
using SPT.Common.Utils;
using SPT.Custom.Models;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.EssentialPatches
{
    /// <summary>
    /// Originally developed by SPT team
    /// </summary>
    public class FikaVersionLabel_Patch : ModulePatch
    {
        private static string versionLabel;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(VersionNumberClass).GetMethod(nameof(VersionNumberClass.Create),
                BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPostfix]
        internal static void PatchPostfix(string major, object __result)
        {
            FikaPlugin.EFTVersionMajor = major;

            if (string.IsNullOrEmpty(versionLabel))
            {
                string json = RequestHandler.GetJson("/singleplayer/settings/version");
                versionLabel = Json.Deserialize<VersionResponse>(json).Version;
                Logger.LogInfo($"Server version: {versionLabel}");
            }

            string fikaVersion = Assembly.GetAssembly(typeof(FikaVersionLabel_Patch)).GetName().Version.ToString();

            Traverse preloaderUiTraverse = Traverse.Create(MonoBehaviourSingleton<PreloaderUI>.Instance);

            preloaderUiTraverse.Field("_alphaVersionLabel").Property("LocalizationKey").SetValue("{0}");

            var versionNumberTraverse = Traverse.Create(__result);
            if (!FikaPlugin.OfficialVersion.Value)
            {
                preloaderUiTraverse.Field("string_2").SetValue($"FIKA BETA {fikaVersion} | {versionLabel}");
                versionNumberTraverse.Field("Major").SetValue($"{fikaVersion} {versionLabel}");
            }

            //Game version
            // preloaderUiTraverse.Field("string_2").SetValue($"Game version");
            //Raid code
            // preloaderUiTraverse.Field("string_3").SetValue($"Raid code");
            //Game mode
            preloaderUiTraverse.Field("string_4").SetValue("PvE");
        }
    }
}