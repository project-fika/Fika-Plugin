using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    public class TransitControllerAbstractClass_Exist_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TransitControllerAbstractClass)
                .GetMethod(nameof(TransitControllerAbstractClass.Exist))
                .MakeGenericMethod(typeof(TransitInteractionControllerAbstractClass));
        }

        [PatchPrefix]
        public static bool Prefix(ref bool __result, ref TransitControllerAbstractClass transitController)
        {
            if (FikaGlobals.IsInRaid)
            {
                GameWorld gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld != null)
                {
                    transitController = gameWorld.TransitController;
                    if (transitController != null)
                    {
                        __result = true;
                    }
                }
                return false;
            }

            return true;
        }
    }
}
