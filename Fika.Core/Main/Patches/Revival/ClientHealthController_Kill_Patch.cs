using System.Reflection;
using EFT;
using Fika.Core.Main.ClientClasses;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Revival;

[IgnoreAutoPatch]
internal sealed class ClientHealthController_Kill_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ClientHealthController)
            .GetMethod(nameof(ClientHealthController.Kill));
    }

    [PatchPrefix]
    public static bool Prefix(ClientHealthController __instance, EDamageType damageType)
    {
        if (__instance.Player.IsYourPlayer && __instance.ReviveEnabled && __instance.CanBeDowned && !__instance.CheckIfDamageShouldInstantKill())
        {
            __instance.IsAlive = false;
            __instance.method_35(damageType);
            return false;
        }

        return true;
    }
}
