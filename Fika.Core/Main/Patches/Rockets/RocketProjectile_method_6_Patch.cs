using EFT.RocketLauncher;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Rockets;

/// <summary>
/// Do not run method unless server to avoid double damage
/// </summary>
public class RocketProjectile_method_6_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RocketProjectile)
            .GetMethod(nameof(RocketProjectile.method_6));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return FikaBackendUtils.IsServer;
    }
}
