using System.Reflection;
using Aki.Reflection.Patching;

namespace Fika.Headless.Patches
{
    // Token: 0x0200000B RID: 11
    public class VRAMPatch3 : ModulePatch
    {
        // Token: 0x06000020 RID: 32 RVA: 0x000024BC File Offset: 0x000006BC
        protected override MethodBase GetTargetMethod()
        {
            return typeof(CameraClass).GetMethod("GetVRamUsage");
        }

        // Token: 0x06000021 RID: 33 RVA: 0x000024E4 File Offset: 0x000006E4
        [PatchPrefix]
        private static bool Prefix()
        {
            return false;
        }
    }
}
