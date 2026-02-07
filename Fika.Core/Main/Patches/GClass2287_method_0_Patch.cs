using System.Reflection;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches;

internal class GClass2287_method_0_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GClass2287)
            .GetMethod(nameof(GClass2287.method_0));
    }

    [PatchPrefix]
    public static void Prefix(ref ServerScenesDataStruct preset)
    {
        if (FikaBackendUtils.IsClient)
        {
            Logger.LogInfo("Disabling server scenes");
            preset.DisableServerScenes = true;
        }
    }
}
