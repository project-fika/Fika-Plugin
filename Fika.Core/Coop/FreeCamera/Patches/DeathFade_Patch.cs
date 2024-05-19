using Aki.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.FreeCamera.Patches
{
    public class DeathFade_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DeathFade).GetMethod(nameof(DeathFade.DisableEffect));
        }

        [PatchPrefix]
        private static bool Prefix(DeathFade __instance)
        {
            Type deathFadeType = typeof(DeathFade);

            deathFadeType.GetField("_float_0", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(__instance, (float)deathFadeType.GetField("_disableTime", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance));
            deathFadeType.GetField("bool_0", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(__instance, false);
            AnimationCurve disableCurveValue = (AnimationCurve)deathFadeType.GetField("_disableCurve", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            deathFadeType.GetField("animationCurve_0", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(__instance, disableCurveValue);
            return false;
        }
    }
}