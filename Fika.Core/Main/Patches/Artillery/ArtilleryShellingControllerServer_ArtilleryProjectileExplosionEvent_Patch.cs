using CommonAssets.Scripts.ArtilleryShelling;
using System.Reflection;
using SPTushonka.Reflection.Patching;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Patches.Artillery;

public class ArtilleryShellingControllerServer_ArtilleryProjectileExplosionEvent_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ArtilleryShellingControllerServer).GetMethod(nameof(ArtilleryShellingControllerServer.ArtilleryProjectileExplosionEvent));
    }

    [PatchPrefix]
    public static void Prefix(ArtilleryShellingControllerServer __instance, ArtilleryProjectileServer serverProjectile)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        __instance.WriteArtilleryExplosionProjectilePacket(serverProjectile);
    }
}
