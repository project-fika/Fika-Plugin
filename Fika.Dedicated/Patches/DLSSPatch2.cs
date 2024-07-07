using System.Reflection;
using SPT.Reflection.Patching;

namespace Fika.Dedicated.Patches
{
    // Token: 0x02000005 RID: 5
    public class DLSSPatch2 : ModulePatch
    {
        // Token: 0x0600000E RID: 14 RVA: 0x00002288 File Offset: 0x00000488
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DLSSWrapper).GetMethod("IsDLSSLibraryLoaded");
        }

        // Token: 0x0600000F RID: 15 RVA: 0x000022B0 File Offset: 0x000004B0
        [PatchPrefix]
        private static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}
