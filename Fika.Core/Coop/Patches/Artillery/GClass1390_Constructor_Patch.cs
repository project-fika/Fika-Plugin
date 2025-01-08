using Comfort.Common;
using Fika.Core.Coop.GameMode;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass1390_Constructor_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1390).GetConstructors().Single();
        }

        [PatchPrefix]
        public static bool Prefix(GClass1390 __instance, ref GClass1389 ___gclass1389_0)
        {
            __instance.speed = 50f;
            __instance.arcHeight = -150f;
            __instance.explosionDistnaceRange = new(3f, 5f);
            __instance.zoneID = "";
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
            coopGame.UpdateByUnity += __instance.OnUpdate;
            ___gclass1389_0 = new();
            return false;
        }
    }
}
