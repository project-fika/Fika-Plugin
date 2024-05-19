using Aki.Common.Http;
using Aki.Reflection.Patching;
using EFT;
using Fika.Core.Coop.Matchmaker;
using System.Reflection;

namespace Fika.Core.AkiSupport.Overrides
{
    public class BotDifficultyPatch_Override : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass532).GetMethod(nameof(GClass532.LoadDifficultyStringInternal));
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref string __result, BotDifficulty botDifficulty, WildSpawnType role)
        {
            if (MatchmakerAcceptPatches.IsServer)
            {
                __result = RequestHandler.GetJson($"/singleplayer/settings/bot/difficulty/{role}/{botDifficulty}");
                bool resultIsNullEmpty = string.IsNullOrWhiteSpace(__result);
                if (resultIsNullEmpty)
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"BotDifficultyPatchOverride: Unable to get difficulty settings for {role} {botDifficulty}");
                }

                return resultIsNullEmpty; // Server data returned = false = skip original method 
            }
            else
            {
                return false;
            }
        }
    }
}
