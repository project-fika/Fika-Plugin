using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.RocketLauncher;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Rockets;

/// <summary>
/// Do not run method unless server to avoid double damage
/// </summary>
public class RocketProjectile_Launch_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RocketProjectile)
            .GetMethod(nameof(RocketProjectile.Launch));
    }

    [PatchPrefix]
    public static bool Prefix(RocketProjectile __instance)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        if (Singleton<GameWorld>.Instance is ClientLocalGameWorld && FikaBackendUtils.IsServer)
        {
            __instance._coneBlastCoroutine = __instance.StartCoroutine(__instance._backBlastModel.ConeBlast(__instance._coneBlastCoroutine));
        }
        __instance.CreateShot();
        __instance.SetVisibleModel(true);
        __instance._isLaunched = true;

        return false;
    }
}
