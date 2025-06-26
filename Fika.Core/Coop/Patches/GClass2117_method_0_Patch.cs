using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    internal class GClass2117_method_0_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2117).GetMethod(nameof(GClass2117.method_0));
        }

        [PatchPrefix]
        public static void Prefix(ref ServerScenesDataStruct preset)
        {
            if (FikaBackendUtils.IsClient)
            {
                Logger.LogInfo("Disabling server scenes");
                preset.DisableServerScenes = true;
            }
        }
    }
}
