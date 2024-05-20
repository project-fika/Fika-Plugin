using Aki.Reflection.Patching;
using EFT;
using Fika.Core.Coop.Matchmaker;
using System.Reflection;

namespace Fika.Core.Coop.Patches.LocalGame
{
    internal class WaveSpawnScenario_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(WavesSpawnScenario).GetMethod(nameof(WavesSpawnScenario.Run));

        [PatchPrefix]
        public static bool PatchPrefix(WavesSpawnScenario __instance)
        {
            var result = MatchmakerAcceptPatches.IsServer;
            typeof(WavesSpawnScenario).GetProperty(nameof(WavesSpawnScenario.Enabled)).SetValue(__instance, result);
            return result;
        }
    }
}
