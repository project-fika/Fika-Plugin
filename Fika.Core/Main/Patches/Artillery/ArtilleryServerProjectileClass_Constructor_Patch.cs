using Comfort.Common;
using Fika.Core.Main.GameMode;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Main.Patches.Artillery;

public class ArtilleryServerProjectileClass_Constructor_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ArtilleryServerProjectileClass).GetConstructors().Single();
    }

    [PatchPrefix]
    public static bool Prefix(ArtilleryServerProjectileClass __instance)
    {
        __instance.speed = 50f;
        __instance.arcHeight = -150f;
        __instance.explosionDistnaceRange = new(3f, 5f);
        __instance.zoneID = "";
        IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
        (fikaGame.GameController as HostGameController).UpdateByUnity += __instance.OnUpdate;
        __instance.MineDataClass = new();
        return false;
    }
}
