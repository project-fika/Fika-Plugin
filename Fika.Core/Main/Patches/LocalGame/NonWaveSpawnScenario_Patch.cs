using System.Reflection;
using EFT;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.LocalGame;

internal class NonWaveSpawnScenario_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => typeof(NonWavesSpawnScenario).GetMethod(nameof(NonWavesSpawnScenario.Run));

    [PatchPrefix]
    public static bool PatchPrefix(NonWavesSpawnScenario __instance)
    {
        var result = FikaBackendUtils.IsServer;
        typeof(NonWavesSpawnScenario).GetProperty(nameof(NonWavesSpawnScenario.Enabled)).SetValue(__instance, result);
        return result;
    }
}
