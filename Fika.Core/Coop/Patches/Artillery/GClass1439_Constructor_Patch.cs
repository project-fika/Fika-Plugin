using Fika.Core.Coop.GameMode;
using Fika.Core.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass1439_Constructor_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1439).GetConstructors().Single();
        }

        [PatchPrefix]
        public static bool Prefix(GClass1439 __instance, ref GClass1438 ___Gclass1438_0)
        {
            __instance.speed = 50f;
            __instance.arcHeight = -150f;
            __instance.explosionDistnaceRange = new(3f, 5f);
            __instance.zoneID = "";
            CoopGame coopGame = CoopGame.Instance;
            coopGame.UpdateByUnity += __instance.OnUpdate;
            ___Gclass1438_0 = new();
            return false;
        }
    }
}
