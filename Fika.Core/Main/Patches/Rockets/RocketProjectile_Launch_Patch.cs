using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.RocketLauncher;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

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
    public static bool Prefix(RocketProjectile __instance, ref bool ____isLaunched, ref Coroutine ____coneBlastCoroutine, BackblastModel ____backBlastModel)
    {
        if (Singleton<GameWorld>.Instance is ClientLocalGameWorld && FikaBackendUtils.IsServer)
        {
            ____coneBlastCoroutine = __instance.StartCoroutine(____backBlastModel.ConeBlast(____coneBlastCoroutine));
        }
        __instance.CreateShot();
        __instance.SetVisibleModel(true);
        ____isLaunched = true;

        return false;
    }
}
