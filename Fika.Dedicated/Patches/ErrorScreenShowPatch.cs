using System.Reflection;
using EFT.UI;
using Aki.Reflection.Patching;

namespace Fika.Headless.Patches
{
    // Token: 0x02000008 RID: 8
    internal class ErrorScreenShowPatch : ModulePatch
    {
        // Token: 0x06000017 RID: 23 RVA: 0x0000237C File Offset: 0x0000057C
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ErrorScreen).GetMethod("Show");
        }

        // Token: 0x06000018 RID: 24 RVA: 0x000023A4 File Offset: 0x000005A4
        [PatchPrefix]
        private static bool Prefix(string message)
        {
            Logger.LogError("ErrorScreen.Show: " + message);
            return false;
        }
    }
}
