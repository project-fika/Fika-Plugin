using Aki.Reflection.Patching;
using EFT.UI.SessionEnd;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.UI;

namespace Fika.Headless.Patches
{
    public class HealthTreamentScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealthTreatmentScreen).GetMethod(nameof(HealthTreatmentScreen.IsAvailable), BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        static bool PatchPrefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}
