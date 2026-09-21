using System.Reflection;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.FreeCamera.Patches;

public sealed class DeathFade_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(DeathFade)
            .GetMethod(nameof(DeathFade.DisableEffect));
    }

    [PatchPrefix]
    private static bool Prefix(DeathFade __instance)
    {
        __instance._time = __instance._disableTime;
        __instance._isDead = false;
        __instance._deathTimer = 0f;
        __instance._currentCurve = __instance._disableCurve;
        return false;
    }
}