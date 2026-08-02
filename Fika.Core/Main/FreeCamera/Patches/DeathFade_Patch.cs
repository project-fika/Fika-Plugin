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
            ?.SetValue(__instance, (float)deathFadeType.GetField("_disableTime", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance));
        deathFadeType.GetField("_isDead", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(__instance, false);
        var disableCurveValue = (AnimationCurve)deathFadeType.GetField("_disableCurve", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
        deathFadeType.GetField("_currentCurve", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(__instance, disableCurveValue);
        return false;
    }
}