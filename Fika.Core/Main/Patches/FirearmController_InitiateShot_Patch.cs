using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ClientClasses.HandsControllers;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches;

public class FirearmController_InitiateShot_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.InitiateShot));
    }

    [PatchPrefix]
    public static void Prefix(Player.FirearmController __instance, IWeapon weapon, Ammo ammo, Vector3 shotPosition,
        Vector3 shotDirection, Vector3 fireportPosition, int chamberIndex, float overheat)
    {
        if (__instance is FikaClientFirearmController controller)
        {
            controller.SendShotInfo(weapon, ammo, shotPosition, shotDirection, fireportPosition, chamberIndex, overheat);
        }
    }
}
