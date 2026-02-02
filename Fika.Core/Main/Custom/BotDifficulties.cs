using System.Collections.Generic;
using EFT;
using Fika.Core.Main.Utils;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace Fika.Core.Main.Custom;

public class BotDifficulties : Dictionary<string, BotDifficulties.RoleData>
{
    public CoreBotSettingsClass CoreSettings
    {
        get
        {
            FikaGlobals.LogInfo("Retrieving Core settings");
            return _coreSettings ?? null;
        }
    }

    [JsonIgnore]
    private readonly CoreBotSettingsClass _coreSettings;

    public BotDifficulties()
    {
        var coreString = RequestHandler.GetJson("/singleplayer/settings/bot/difficulty/core/core");
        _coreSettings = JsonConvert.DeserializeObject<CoreBotSettingsClass>(coreString);

        // Adjust wave coefs so that wave settings do something
        _coreSettings.WAVE_COEF_LOW = 0.5f;
        _coreSettings.WAVE_COEF_HIGH = 1.5f;
    }

    public BotSettingsComponents GetComponent(BotDifficulty botDifficulty, WildSpawnType role)
    {
#if DEBUG
        FikaGlobals.LogInfo($"Retrieving data for: {role}, difficulty: {botDifficulty}");
#endif
        if (TryGetValue(role.ToString().ToLower(), out var value))
        {
            if (value.TryGetValue(botDifficulty.ToString().ToLower(), out var botSettingsComponents))
            {
                return botSettingsComponents;
            }
        }

        FikaGlobals.LogError($"Unable to retrieve difficulty settings for: {role}, difficulty: {botDifficulty}");
        return null;
    }

    public class RoleData : Dictionary<string, BotSettingsComponents>
    {

    }
}
