using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.TransitController;

public class TransitController_Exist_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(EFT.TransitController)
            .GetMethod(nameof(EFT.TransitController.Exist))
            .MakeGenericMethod(typeof(ClientTransitController));
    }

    [PatchPrefix]
    public static bool Prefix(ref bool __result, ref EFT.TransitController transitController)
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
