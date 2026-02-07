using System.Reflection;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Rockets;

/// <summary>
/// Do not run method unless server to avoid double damage
/// </summary>
public class RocketLauncherConeBlastClass_Initialize_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RocketLauncherConeBlastClass)
            .GetMethod(nameof(RocketLauncherConeBlastClass.Initialize));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return FikaBackendUtils.IsServer;
    }
}
