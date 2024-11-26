using EFT;
using Newtonsoft.Json;
using SPT.Common.Http;
using System.Collections.Generic;

namespace Fika.Core.Coop.Custom
{
	public class BotDifficulties : Dictionary<string, BotDifficulties.RoleData>
	{
		[JsonIgnore]
		private CoreBotSettingsClass coreSettings;

		public BotDifficulties()
		{
			string coreString = RequestHandler.GetJson("/singleplayer/settings/bot/difficulty/core/core");
			coreSettings = JsonConvert.DeserializeObject<CoreBotSettingsClass>(coreString);

			// Adjust wave coefs so that wave settings do something
			coreSettings.WAVE_COEF_LOW = 0.5f;
			coreSettings.WAVE_COEF_HIGH = 1.5f;
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
			if (coreSettings != null)
			{
				return coreSettings;
			}

			return null;
		}

		public class RoleData : Dictionary<string, BotSettingsComponents>
		{

		}
	}
}
