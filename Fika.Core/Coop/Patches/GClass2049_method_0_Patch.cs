using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    internal class GClass2049_method_0_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2049).GetMethod(nameof(GClass2049.method_0));
        }

        [PatchPrefix]
        public static void Prefix(ref GStruct242 preset)
        {
            if (FikaBackendUtils.IsClient)
            {
                Logger.LogInfo("Disabling server scenes");
                preset.DisableServerScenes = true;
            }
        }
    }
}
