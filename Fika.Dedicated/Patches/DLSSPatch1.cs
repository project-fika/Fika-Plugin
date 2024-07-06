using System.Reflection;
using Aki.Reflection.Patching;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fika.Headless.Patches
{
    // Token: 0x02000004 RID: 4
    public class DLSSPatch1 : ModulePatch
    {
        // Token: 0x0600000B RID: 11 RVA: 0x0000223C File Offset: 0x0000043C
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SystemInfo).GetProperty("graphicsDeviceType").GetGetMethod();
        }

        // Token: 0x0600000C RID: 12 RVA: 0x00002268 File Offset: 0x00000468
        [PatchPrefix]
        private static bool Prefix(ref GraphicsDeviceType __result)
        {
            __result = GraphicsDeviceType.Direct3D11;
            return false;
        }
    }
}
