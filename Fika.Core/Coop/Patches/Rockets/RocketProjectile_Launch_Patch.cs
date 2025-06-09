using Comfort.Common;
using EFT;
using EFT.RocketLauncher;
using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches.Rockets
{
    /// <summary>
    /// Do not run method unless server to avoid double damage
    /// </summary>
    public class RocketProjectile_Launch_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(RocketProjectile)
                .GetMethod(nameof(RocketProjectile.Launch));
        }

        [PatchPrefix]
        public static bool Prefix(RocketProjectile __instance, ref bool ___bool_0, ref Coroutine ___coroutine_0, GClass3734 ___gclass3734_0)
        {
            if (Singleton<GameWorld>.Instance is ClientLocalGameWorld && FikaBackendUtils.IsServer)
            {
                ___coroutine_0 = __instance.StartCoroutine(___gclass3734_0.ConeBlast(___coroutine_0));
            }
            __instance.method_1();
            __instance.method_10(true);
            ___bool_0 = true;

            return false;
        }
    }
}
