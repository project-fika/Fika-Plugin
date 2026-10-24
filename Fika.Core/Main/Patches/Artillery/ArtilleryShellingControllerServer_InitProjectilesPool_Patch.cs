using System.Reflection;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Artillery;

public class ArtilleryShellingControllerServer_InitProjectilesPool_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ArtilleryShellingControllerServer).GetMethod(nameof(ArtilleryShellingControllerServer.InitProjectilesPool));
    }

    [PatchPostfix]
    public static void Postfix(ArtilleryShellingControllerServer __instance)
    {
        if (FikaBackendUtils.IsTutorial || FikaGlobals.FikaGame?.GameController is not HostGameController host)
        {
            return;
        }

        __instance._offlineMode = true;
        host.add_UpdateByUnity(FikaGlobals.Il2CppActionFor(__instance, nameof(__instance.OnUpdate)));
    }
}
