using CommonAssets.Scripts.ArtilleryShelling;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using Fika.Core.Main.GameMode;
using SPTushonka.Reflection.Patching;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Patches.Artillery;

public class ArtilleryProjectileServer_Constructor_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ArtilleryProjectileServer).GetConstructor(System.Type.EmptyTypes);
    }

    [PatchPrefix]
    public static bool Prefix(ArtilleryProjectileServer __instance)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        __instance.speed = 50f;
        __instance.arcHeight = -150f;
        __instance.explosionDistnaceRange = new(3f, 5f);
        __instance.zoneID = "";
        var fikaGame = FikaGlobals.FikaGame;
        (fikaGame.GameController as HostGameController).add_UpdateByUnity(FikaGlobals.Il2CppActionFor(__instance, nameof(__instance.OnUpdate)));
        __instance._explosiveItem = new();
        return false;
    }
}
