using System.Reflection;
using EFT;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.LocalGame;

internal class WaveSpawnScenario_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => typeof(WavesSpawnScenario).GetMethod(nameof(WavesSpawnScenario.Run));

    [PatchPrefix]
    public static bool PatchPrefix(WavesSpawnScenario __instance)
    {
        var result = FikaBackendUtils.IsServer;
        typeof(WavesSpawnScenario).GetProperty(nameof(WavesSpawnScenario.Enabled)).SetValue(__instance, result);
        return result;
    }
}
