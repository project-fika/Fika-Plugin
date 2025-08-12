using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Artillery;

public class ServerShellingControllerClass_method_28_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ServerShellingControllerClass).GetMethod(nameof(ServerShellingControllerClass.method_28));
    }

    [PatchPrefix]
    public static void Prefix(ServerShellingControllerClass __instance, ArtilleryServerProjectileClass serverProjectile)
    {
        __instance.method_31(serverProjectile);
    }
}
