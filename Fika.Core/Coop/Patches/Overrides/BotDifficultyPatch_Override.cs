using EFT;
using Fika.Core.Coop.Utils;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Overrides
{
	public class BotDifficultyPatch_Override : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass531).GetMethod(nameof(GClass531.LoadDifficultyStringInternal));
		}

		[PatchPrefix]
		private static bool PatchPrefix(ref string __result, BotDifficulty botDifficulty, WildSpawnType role)
		{
			if (FikaBackendUtils.IsServer)
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
