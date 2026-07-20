using System.Reflection;
using EFT.RocketLauncher;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Rockets;

/// <summary>
/// Do not run method unless server to avoid double damage
/// </summary>
public class RocketProjectile_TryVehicleCollision_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RocketProjectile)
            .GetMethod(nameof(RocketProjectile.TryVehicleCollision));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return FikaBackendUtils.IsServer;
    }
}
