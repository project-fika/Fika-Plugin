using EFT;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class BotCacher_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass531).GetMethod(nameof(GClass531.LoadInternal), BindingFlags.Static | BindingFlags.Public);
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
                string text = GClass531.LoadCoreByString();
                if (text == null)
                {
                    core = null;
                    __result = false;
                    return false;
                }
                core = CoreBotSettingsClass.Create(text);
            }

            if (FikaBackendUtils.IsServer)
            {
                foreach (object type in Enum.GetValues(typeof(WildSpawnType)))
                {
                    foreach (object difficulty in Enum.GetValues(typeof(BotDifficulty)))
                    {
                        BotSettingsComponents botSettingsComponents;
                        botSettingsComponents = FikaPlugin.Instance.BotDifficulties.GetComponent((BotDifficulty)difficulty, (WildSpawnType)type);
                        if (botSettingsComponents != null)
                        {
                            if (!GClass531.AllSettings.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
                            {
                                GClass531.AllSettings.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
                            }
                        }
                        else
                        {
                            botSettingsComponents = GClass531.smethod_1(GClass531.CheckOnExclude((BotDifficulty)difficulty, (WildSpawnType)type), (WildSpawnType)type, false);
                            if (botSettingsComponents != null)
                            {
                                if (!GClass531.AllSettings.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
                                {
                                    GClass531.AllSettings.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
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
            }

            return false;
        }
    }
}
