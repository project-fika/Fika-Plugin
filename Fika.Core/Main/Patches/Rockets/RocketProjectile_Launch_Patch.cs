using Comfort.Common;
using EFT;
using EFT.RocketLauncher;
using Fika.Core.Main.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Rockets
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
        public static bool Prefix(RocketProjectile __instance, ref bool ___bool_0, ref Coroutine ___coroutine_0, RocketLauncherConeBlastClass ___rocketLauncherConeBlastClass)
        {
            if (Singleton<GameWorld>.Instance is ClientLocalGameWorld && FikaBackendUtils.IsServer)
            {
                ___coroutine_0 = __instance.StartCoroutine(___rocketLauncherConeBlastClass.ConeBlast(___coroutine_0));
            }
            __instance.method_1();
            __instance.method_11(true);
            ___bool_0 = true;

            return false;
        }
    }
}
