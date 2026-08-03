using CommonAssets.Scripts.ArtilleryShelling;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using Fika.Core.Main.GameMode;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Artillery;

public class ArtilleryProjectileServer_Constructor_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ArtilleryProjectileServer).GetConstructors().Single();
    }

    [PatchPrefix]
    public static bool Prefix(ArtilleryProjectileServer __instance)
    {
        __instance.speed = 50f;
        __instance.arcHeight = -150f;
        __instance.explosionDistnaceRange = new(3f, 5f);
        __instance.zoneID = "";
        var fikaGame = Singleton<IFikaGame>.Instance;
        (fikaGame.GameController as HostGameController).UpdateByUnity += __instance.OnUpdate;
        __instance._explosiveItem = new();
        return false;
    }
}
