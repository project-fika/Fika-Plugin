using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.LocalGame;

internal class NonWaveSpawnScenario_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod() => typeof(NonWavesSpawnScenario).GetMethod(nameof(NonWavesSpawnScenario.Run));

    [PatchPrefix]
    public static bool PatchPrefix(NonWavesSpawnScenario __instance)
    {
        bool result = FikaBackendUtils.IsServer;
        typeof(NonWavesSpawnScenario).GetProperty(nameof(NonWavesSpawnScenario.Enabled)).SetValue(__instance, result);
        return result;
    }
}
