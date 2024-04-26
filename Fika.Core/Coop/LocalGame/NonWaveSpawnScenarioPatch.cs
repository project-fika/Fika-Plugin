using Aki.Reflection.Patching;
using EFT;
using Fika.Core.Coop.Matchmaker;
using System.Reflection;

namespace Fika.Core.Coop.LocalGame
{
    internal class NonWaveSpawnScenarioPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(NonWavesSpawnScenario).GetMethod(nameof(NonWavesSpawnScenario.Run));

        [PatchPrefix]
        public static bool PatchPrefix(NonWavesSpawnScenario __instance)
        {
            var result = MatchmakerAcceptPatches.IsServer;
            typeof(NonWavesSpawnScenario).GetProperty(nameof(NonWavesSpawnScenario.Enabled)).SetValue(__instance, result);
            return result;
        }
    }
}
