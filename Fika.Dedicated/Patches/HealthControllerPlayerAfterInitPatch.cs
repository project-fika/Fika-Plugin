using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using EFT.HealthSystem;
using UnityEngine;

namespace Fika.Dedicated.Patches
{
    public class HealthControllerPlayerAfterInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod(nameof(Player.Init));
        }

        [PatchPostfix]
        private static void Postfix(Player __instance)
        {
            if (__instance.IsYourPlayer)
            {
                ActiveHealthController healthController = __instance.ActiveHealthController;
                if (healthController != null)
                {
                    healthController.SetDamageCoeff(0f);
                    healthController.DisableMetabolism();
                }
            }

            Vector3 currentPosition = __instance.Position;
            __instance.Teleport(new(currentPosition.x, currentPosition.y - 50f, currentPosition.z));
        }
    }
}
