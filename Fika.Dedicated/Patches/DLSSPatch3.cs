using System.Reflection;
using SPT.Reflection.Patching;

namespace Fika.Dedicated.Patches
{
    // Token: 0x02000006 RID: 6
    public class DLSSPatch3 : ModulePatch
    {
        // Token: 0x06000011 RID: 17 RVA: 0x000022D0 File Offset: 0x000004D0
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DLSSWrapper).GetMethod("IsDLSSSupported");
        }

        // Token: 0x06000012 RID: 18 RVA: 0x000022F8 File Offset: 0x000004F8
        [PatchPrefix]
        private static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}
