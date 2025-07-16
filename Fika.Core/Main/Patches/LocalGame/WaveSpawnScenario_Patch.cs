using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    internal class WaveSpawnScenario_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod() => typeof(WavesSpawnScenario).GetMethod(nameof(WavesSpawnScenario.Run));

        [PatchPrefix]
        public static bool PatchPrefix(WavesSpawnScenario __instance)
        {
            bool result = FikaBackendUtils.IsServer;
            typeof(WavesSpawnScenario).GetProperty(nameof(WavesSpawnScenario.Enabled)).SetValue(__instance, result);
            return result;
        }
    }
}
