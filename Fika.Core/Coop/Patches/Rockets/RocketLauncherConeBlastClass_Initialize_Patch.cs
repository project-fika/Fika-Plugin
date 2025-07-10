using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Rockets
{
    /// <summary>
    /// Do not run method unless server to avoid double damage
    /// </summary>
    public class RocketLauncherConeBlastClass_Initialize_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(RocketLauncherConeBlastClass)
                .GetMethod(nameof(RocketLauncherConeBlastClass.Initialize));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return FikaBackendUtils.IsServer;
        }
    }
}
