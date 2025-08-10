using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Comfort.Common;
using Diz.Utils;
using EFT.UI;
using Fika.Core.Main.Patches;
using Fika.Core.Networking.Http;
using Fika.Core.Patching;
using LiteNetLib.Utils;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Custom.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Fika.Core.Utils
{
    /// <summary>
    /// Class used to verify and handle other SPT mods
    /// </summary>
    public class FikaModHandler
    {
        private readonly ManualLogSource _logger = Logger.CreateLogSource("FikaModHandler");

        public bool QuestingBotsLoaded;
        public bool SAINLoaded;
        public bool UIFixesLoaded;

        public Version SPTCoreVersion { get; private set; }

        public FikaModHandler()
        {
            Chainloader.PluginInfos.TryGetValue("com.SPT.core", out PluginInfo pluginInfo);
            SPTCoreVersion = pluginInfo.Metadata.Version;
        }

        public async Task VerifyMods(PatchManager manager)
        {
            PluginInfo[] pluginInfos = [.. Chainloader.PluginInfos.Values];

            // Set capacity to avoid unnecessarily resizing for people who have a lot of mods loaded
            Dictionary<string, uint> loadedMods = new(pluginInfos.Length);

            foreach (PluginInfo pluginInfo in pluginInfos)
            {
                string location = pluginInfo.Location;
                byte[] fileBytes = File.ReadAllBytes(location);
                uint crc32 = CRC32C.Compute(fileBytes, 0, fileBytes.Length);
                loadedMods.Add(pluginInfo.Metadata.GUID, crc32);
                _logger.LogInfo($"Loaded plugin: [{pluginInfo.Metadata.Name}] with GUID [{pluginInfo.Metadata.GUID}] and crc32 [{crc32}]");
                if (pluginInfo.Metadata.GUID == "com.fika.core")
                {
                    FikaPlugin.Crc32 = crc32;
                }

                CheckSpecialMods(pluginInfo.Metadata.GUID);
            }

            string modValidationRequestJson = JsonConvert.SerializeObject(loadedMods);
            _logger.LogDebug(modValidationRequestJson);

            string validationJson = RequestHandler.PostJson("/fika/client/check/mods", modValidationRequestJson);
            _logger.LogDebug(validationJson);

            ModValidationResponse validationResult = JsonConvert.DeserializeObject<ModValidationResponse>(validationJson);
            if (validationResult.Forbidden == null || validationResult.MissingRequired == null || validationResult.HashMismatch == null)
            {
                FikaPlugin.Instance.FikaLogger.LogError("FikaModHandler::VerifyMods: Response was invalid!");
                MessageBoxHelper.Show($"Failed to verify mods with server.\nMake sure that the server mod is installed!", "FIKA ERROR", MessageBoxHelper.MessageBoxType.OK);
                AsyncWorker.RunInMainTread(Application.Quit);
                return;
            }

            // If any errors were detected we will print what has happened
            bool installationError =
                validationResult.Forbidden.Length > 0 ||
                validationResult.MissingRequired.Length > 0 ||
                validationResult.HashMismatch.Length > 0;

            if (validationResult.Forbidden.Length > 0)
            {
                _logger.LogError($"{validationResult.Forbidden.Length} forbidden mod(s) are loaded, have the server host allow or remove the following mods: {string.Join(", ", validationResult.Forbidden)}");
            }

            if (validationResult.MissingRequired.Length > 0)
            {
                _logger.LogError($"{validationResult.MissingRequired.Length} missing required mod(s), verify the following mods are present: {string.Join(", ", validationResult.MissingRequired)}");
            }

            if (validationResult.HashMismatch.Length > 0)
            {
                _logger.LogWarning($"{validationResult.HashMismatch.Length} mismatched mod(s) are loaded, verify the following mods are up to date with the server host: {string.Join(", ", validationResult.HashMismatch)}");
            }

            if (installationError)
            {
                _ = Task.Run(InformInstallationError);
            }

            await HandleModSpecificPatches(manager);

            return;
        }

        private Task HandleModSpecificPatches(PatchManager manager)
        {
            // We only want to load this if UI Fixes is not loaded
            if (!UIFixesLoaded)
            {
                _logger.LogInfo("UI Fixes is not loaded, enabling PartyInfoPanel fix");
                manager.EnablePatch(new PartyInfoPanel_method_3_Patch());
            }

            return Task.CompletedTask;
        }

        private async Task InformInstallationError()
        {
            while (!Singleton<PreloaderUI>.Instantiated)
            {
                await Task.Delay(250);
            }

            AsyncWorker.RunInMainTread(ShowModErrorMessage);
        }

        private void ShowModErrorMessage()
        {
            string message = "Your client doesn't meet server requirements, check logs for more details";

            // -1f time makes the message permanent
            GClass3629 errorScreen = Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("INSTALLATION ERROR", message,
                ErrorScreen.EButtonType.QuitButton, -1f);

            Action quitAction = Application.Quit;
            errorScreen.OnAccept += quitAction;
            errorScreen.OnDecline += quitAction;
            errorScreen.OnClose += quitAction;
            errorScreen.OnCloseSilent += quitAction;
        }

        private void CheckSpecialMods(string key)
        {
            switch (key)
            {
                case "com.DanW.QuestingBots":
                    {
                        QuestingBotsLoaded = true;
                        break;
                    }
                case "me.sol.sain":
                    {
                        SAINLoaded = true;
                        break;
                    }
                case "Tyfon.UIFixes":
                    {
                        UIFixesLoaded = true;
                        break;
                    }
            }
        }
    }
}
