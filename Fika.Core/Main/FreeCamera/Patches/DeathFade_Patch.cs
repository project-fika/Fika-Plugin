using System.Reflection;
using SPT.Reflection.Patching;

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
        var deathFadeType = typeof(DeathFade);

        deathFadeType.GetField("_time", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(__instance, __instance._disableTime);
        deathFadeType.GetField("_isDead", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(__instance, false);
        deathFadeType.GetField("_currentCurve", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(__instance, __instance._disableCurve);
        return false;
    }
}