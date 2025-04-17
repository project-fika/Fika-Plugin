using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    internal class GClass2054_method_0_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2054).GetMethod(nameof(GClass2054.method_0));
        }

        [PatchPrefix]
        public static void Prefix(ref GStruct245 preset)
        {
            if (FikaBackendUtils.IsClient)
            {
                Logger.LogInfo("Disabling server scenes");
                preset.DisableServerScenes = true;
            }
        }
    }
}
