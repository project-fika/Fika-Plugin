using EFT;
using Fika.Core.Patching;
using System;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    public class BotCacher_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalBotSettingsProviderClass)
                .GetMethod(nameof(LocalBotSettingsProviderClass.LoadInternal),
                BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(out CoreBotSettingsClass core, ref bool __result)
        {
            if (FikaPlugin.Instance.BotDifficulties != null)
            {
                core = FikaPlugin.Instance.BotDifficulties.CoreSettings;
            }
            else
            {
                string text = LocalBotSettingsProviderClass.LoadCoreByString();
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
                        if (!LocalBotSettingsProviderClass.Gclass621_1.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
                        {
                            LocalBotSettingsProviderClass.Gclass621_1.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
                        }
                    }
                    else
                    {
                        botSettingsComponents = LocalBotSettingsProviderClass.smethod_4(LocalBotSettingsProviderClass.CheckOnExclude((BotDifficulty)difficulty, (WildSpawnType)type), (WildSpawnType)type, false, true);
                        if (botSettingsComponents != null)
                        {
                            if (!LocalBotSettingsProviderClass.Gclass621_1.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
                            {
                                LocalBotSettingsProviderClass.Gclass621_1.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
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
            return false;
        }
    }
}
