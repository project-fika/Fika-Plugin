using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.TransitController;

public class TransitControllerAbstractClass_Exist_Patch : ModulePatch
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
            var gameWorld = Singleton<GameWorld>.Instance;
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
