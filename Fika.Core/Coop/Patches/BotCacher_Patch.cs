using Aki.Reflection.Patching;
using EFT;
using Fika.Core.Coop.Matchmaker;
using System;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class BotCacher_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass532).GetMethod(nameof(GClass532.LoadInternal), BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(out GClass531 core, ref bool __result)
        {
            if (FikaPlugin.Instance.BotDifficulties != null)
            {
                core = FikaPlugin.Instance.BotDifficulties.GetCoreSettings();
            }
            else
            {
                string text = GClass532.LoadCoreByString();
                if (text == null)
                {
                    core = null;
                    __result = false;
                    return false;
                }
                core = GClass531.Create(text);
            }

            if (MatchmakerAcceptPatches.IsServer)
            {
                foreach (object type in Enum.GetValues(typeof(WildSpawnType)))
                {
                    foreach (object difficulty in Enum.GetValues(typeof(BotDifficulty)))
                    {
                        BotSettingsComponents botSettingsComponents;
                        botSettingsComponents = FikaPlugin.Instance.BotDifficulties.GetComponent((BotDifficulty)difficulty, (WildSpawnType)type);
                        if (botSettingsComponents != null)
                        {
                            if (!GClass532.AllSettings.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
                            {
                                GClass532.AllSettings.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
                            }
                        }
                        else
                        {
                            botSettingsComponents = GClass532.smethod_1(GClass532.CheckOnExclude((BotDifficulty)difficulty, (WildSpawnType)type), (WildSpawnType)type, false);
                            if (botSettingsComponents != null)
                            {
                                if (!GClass532.AllSettings.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
                                {
                                    GClass532.AllSettings.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
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
