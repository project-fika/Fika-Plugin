using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class MineDirectional_OnTriggerEnter_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MineDirectional).GetMethod(nameof(MineDirectional.OnTriggerEnter));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            if (FikaBackendUtils.IsClient)
            {
                return false;
            }
            return true;
        }
    }
}
