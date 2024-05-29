using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Fika.Core.Networking.Models;
using LiteNetLib.Utils;
using System.Collections.Generic;
using System.IO;

namespace Fika.Core.Utils
{
    public class FikaModHandler
    {
        private string[] loadedMods;
        private readonly ManualLogSource logger = Logger.CreateLogSource("FikaModHandler");

        public bool QuestingBotsLoaded = false;
        public bool SAINLoaded = false;

        public void Run()
        {
            // Store all loaded plugins (mods) to improve compatibility
            List<string> tempPluginInfos = [];

            foreach (string key in Chainloader.PluginInfos.Keys)
            {
                logger.LogInfo($"Adding {key}, {Chainloader.PluginInfos[key].Metadata.Name} to loaded mods.");
                tempPluginInfos.Add(key);
                CheckSpecialMods(key);
            }

            /*if (FikaPlugin.Instance.RequiredMods.Count > 0)
            {
                VerifyMods();
            }*/

            loadedMods = [.. tempPluginInfos];

            logger.LogInfo($"Loaded {loadedMods.Length} mods!");
        }

        private void VerifyMods()
        {
            PluginInfo[] pluginInfos = [.. Chainloader.PluginInfos.Values];
            Dictionary<string, string> loadedMods = [];

            foreach (PluginInfo pluginInfo in pluginInfos)
            {
                string location = pluginInfo.Location;
                byte[] fileBytes = File.ReadAllBytes(location);
                uint crc32 = CRC32C.Compute(fileBytes, 0, fileBytes.Length);
                loadedMods.Add(pluginInfo.Metadata.GUID, crc32.ToString());
            }

            ModValidationRequest modValidationRequest = new(loadedMods);
            // Send
        }

        private void CheckSpecialMods(string key)
        {
            if (key == "com.DanW.QuestingBots")
            {
                QuestingBotsLoaded = true;
            }

            if (key == "me.sol.sain")
            {
                SAINLoaded = true;
            }
        }
    }
}
