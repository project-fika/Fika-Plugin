using EFT;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class BotCacher_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass565).GetMethod(nameof(GClass565.LoadInternal), BindingFlags.Static | BindingFlags.Public);
		}

		[PatchPrefix]
		private static bool PatchPrefix(out CoreBotSettingsClass core, ref bool __result)
		{
			if (FikaPlugin.Instance.BotDifficulties != null)
			{
				core = FikaPlugin.Instance.BotDifficulties.GetCoreSettings();
			}
			else
			{
				string text = GClass565.LoadCoreByString();
				if (text == null)
				{
					core = null;
					__result = false;
					return false;
				}
				core = CoreBotSettingsClass.Create(text);
			}

			foreach (object type in Enum.GetValues(typeof(WildSpawnType)))
			{
				foreach (object difficulty in Enum.GetValues(typeof(BotDifficulty)))
				{
					BotSettingsComponents botSettingsComponents;
					botSettingsComponents = FikaPlugin.Instance.BotDifficulties.GetComponent((BotDifficulty)difficulty, (WildSpawnType)type);
					if (botSettingsComponents != null)
					{
						if (!GClass565.AllSettings.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
						{
							GClass565.AllSettings.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
						}
					}
					else
					{
						botSettingsComponents = GClass565.smethod_1(GClass565.CheckOnExclude((BotDifficulty)difficulty, (WildSpawnType)type), (WildSpawnType)type, false);
						if (botSettingsComponents != null)
						{
							if (!GClass565.AllSettings.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
							{
								GClass565.AllSettings.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
							}
						}
						else
						{
							__result = false;
							return false;
						}
					}
				}
			}
			__result = true;

			/*if (FikaBackendUtils.IsServer)
			{
				foreach (object type in Enum.GetValues(typeof(WildSpawnType)))
				{
					foreach (object difficulty in Enum.GetValues(typeof(BotDifficulty)))
					{
						BotSettingsComponents botSettingsComponents;
						botSettingsComponents = FikaPlugin.Instance.BotDifficulties.GetComponent((BotDifficulty)difficulty, (WildSpawnType)type);
						if (botSettingsComponents != null)
						{
							if (!GClass565.AllSettings.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
							{
                                GClass565.AllSettings.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
							}
						}
						else
						{
							botSettingsComponents = GClass565.smethod_1(GClass565.CheckOnExclude((BotDifficulty)difficulty, (WildSpawnType)type), (WildSpawnType)type, false);
							if (botSettingsComponents != null)
							{
								if (!GClass565.AllSettings.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
								{
                                    GClass565.AllSettings.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
								}
							}
							else
							{
								__result = false;
								return false;
							}
						}
					}
				}
				__result = true;
			}*/

			return false;
		}
	}
}
