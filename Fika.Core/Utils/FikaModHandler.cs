using BepInEx.Bootstrap;
using BepInEx.Logging;
using System.Collections.Generic;

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

            loadedMods = [.. tempPluginInfos];

            logger.LogInfo($"Loaded {loadedMods.Length} mods!");
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
