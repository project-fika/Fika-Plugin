using System.Reflection;
using EFT;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.LocalGame;

internal class NonWaveSpawnScenario_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => typeof(NonWavesSpawnScenario).GetMethod(nameof(NonWavesSpawnScenario.Run));

    [PatchPrefix]
    public static bool PatchPrefix(NonWavesSpawnScenario __instance)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        var result = FikaBackendUtils.IsServer;
        typeof(NonWavesSpawnScenario).GetProperty(nameof(NonWavesSpawnScenario.Enabled)).SetValue(__instance, result);
        return result;
    }
}
