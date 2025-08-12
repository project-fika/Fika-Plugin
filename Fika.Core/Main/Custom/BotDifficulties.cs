using EFT;
using Newtonsoft.Json;
using SPT.Common.Http;
using System.Collections.Generic;

namespace Fika.Core.Main.Custom;

public class BotDifficulties : Dictionary<string, BotDifficulties.RoleData>
{
    public CoreBotSettingsClass CoreSettings
    {
        get
        {
            FikaPlugin.Instance.FikaLogger.LogInfo("Retrieving Core settings");
            if (_coreSettings != null)
            {
                return _coreSettings;
            }

            return null;
        }
    }

    [JsonIgnore]
    private readonly CoreBotSettingsClass _coreSettings;

    public BotDifficulties()
    {
        string coreString = RequestHandler.GetJson("/singleplayer/settings/bot/difficulty/core/core");
        _coreSettings = JsonConvert.DeserializeObject<CoreBotSettingsClass>(coreString);

        // Adjust wave coefs so that wave settings do something
        _coreSettings.WAVE_COEF_LOW = 0.5f;
        _coreSettings.WAVE_COEF_HIGH = 1.5f;
    }

    public BotSettingsComponents GetComponent(BotDifficulty botDifficulty, WildSpawnType role)
    {
#if DEBUG
        FikaPlugin.Instance.FikaLogger.LogInfo($"Retrieving data for: {role}, difficulty: {botDifficulty}");
#endif
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

    public class RoleData : Dictionary<string, BotSettingsComponents>
    {

    }
}
