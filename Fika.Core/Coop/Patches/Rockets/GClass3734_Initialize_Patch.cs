using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Rockets
{
    /// <summary>
    /// Do not run method unless server to avoid double damage
    /// </summary>
    public class GClass3734_Initialize_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass3734)
                .GetMethod(nameof(GClass3734.Initialize));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return FikaBackendUtils.IsServer;
        }
    }
}
