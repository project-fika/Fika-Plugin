using CommonAssets.Scripts.ArtilleryShelling;
using System.Reflection;
using SPT.Reflection.Patching;

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
        __instance.WriteArtilleryExplosionProjectilePacket(serverProjectile);
    }
}
