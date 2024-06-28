using EFT;
using Newtonsoft.Json;
using SPT.Common.Http;
using System.Collections.Generic;

namespace Fika.Core.Models
{
    public class BotDifficulties : Dictionary<string, BotDifficulties.RoleData>
    {
        [JsonIgnore]
        private CoreBotSettingsClass CoreSettings;

        public BotDifficulties()
        {
            string coreString = RequestHandler.GetJson("/singleplayer/settings/bot/difficulty/core/core");
            CoreSettings = JsonConvert.DeserializeObject<CoreBotSettingsClass>(coreString);
        }

        public BotSettingsComponents GetComponent(BotDifficulty botDifficulty, WildSpawnType role)
        {
            FikaPlugin.Instance.FikaLogger.LogInfo($"Retrieving data for: {role}, difficulty: {botDifficulty}");
            if (TryGetValue(role.ToString().ToLower(), out RoleData value))
            {
                if (value.TryGetValue(botDifficulty.ToString().ToLower(), out BotSettingsComponents botSettingsComponents))
                {
                    return botSettingsComponents;
                }
            }

            FikaPlugin.Instance.FikaLogger.LogError($"Unable to retrieve difficulty settings for: {role}, difficulty: {botDifficulty}");
            return null;
        }

        public CoreBotSettingsClass GetCoreSettings()
        {
            FikaPlugin.Instance.FikaLogger.LogInfo("Retrieving Core settings");
            if (CoreSettings != null)
            {
                return CoreSettings;
            }

            return null;
        }

        public class RoleData : Dictionary<string, BotSettingsComponents>
        {

        }
    }
}
