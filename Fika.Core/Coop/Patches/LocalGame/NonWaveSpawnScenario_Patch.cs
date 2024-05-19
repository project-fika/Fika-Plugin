using Aki.Reflection.Patching;
using EFT;
using Fika.Core.Coop.Matchmaker;
using System.Reflection;

namespace Fika.Core.Coop.Patches.LocalGame
{
    internal class NonWaveSpawnScenario_Patch : ModulePatch
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
