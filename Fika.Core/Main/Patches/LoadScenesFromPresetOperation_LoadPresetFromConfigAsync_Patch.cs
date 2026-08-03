using EFT;
using System.Reflection;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches;

internal class LoadScenesFromPresetOperation_LoadPresetFromConfigAsync_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LoadScenesFromPresetOperation)
            .GetMethod(nameof(LoadScenesFromPresetOperation.LoadPresetFromConfigAsync));
    }

    [PatchPrefix]
    public static void Prefix(ref ScenePresetLoadConfig preset)
    {
        if (FikaBackendUtils.IsClient)
        {
            Logger.LogInfo("Disabling server scenes");
            preset.DisableServerScenes = true;
        }
    }
}
