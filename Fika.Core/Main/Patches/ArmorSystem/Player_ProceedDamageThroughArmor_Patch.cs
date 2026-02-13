using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.ArmorSystem;

/// <summary>
/// This skips recalculation of armors since Fika caches them <br/>
/// It also removes the check for dev balaclava
/// </summary>
internal class Player_ProceedDamageThroughArmor_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player),
            nameof(Player.ProceedDamageThroughArmor));
    }

    [PatchPrefix]
    public static bool Prefix(ref DamageInfoStruct damageInfo, EBodyPartColliderType colliderType,
        EArmorPlateCollider armorPlateCollider, List<ArmorComponent> ____preAllocatedArmorComponents,
        ref List<ArmorComponent> __result, Player __instance, bool damageInfoIsLocal = true)
    {
        List<ArmorComponent> hitArmorList = null;
        var hasBlockedShot = false;
        var hasDeflectedShot = false;

        // 3. Process each armor piece against the incoming shot
        for (var i = 0; i < ____preAllocatedArmorComponents.Count; i++)
        {
            var armor = ____preAllocatedArmorComponents[i];
            var armorDamageDealt = 0f;

            if (armor.ShotMatches(colliderType, armorPlateCollider))
            {
                if (hasBlockedShot || hasDeflectedShot)
                {
                    var bluntThroughput = armor.BluntThroughput;

                    if (armor.ArmorType == EArmorType.Heavy)
                    {
                        bluntThroughput *= (1f - __instance.Skills.HeavyVestBluntThroughputDamageReduction);
                    }

                    damageInfo.Damage *= bluntThroughput;
                }
                else
                {
                    hitArmorList ??= [];
                    hitArmorList.Add(armor);

                    if (__instance.HealthController.IsAlive)
                    {
                        armorDamageDealt = armor.ApplyDamage(
                            ref damageInfo,
                            colliderType,
                            armorPlateCollider,
                            damageInfoIsLocal,
                            ____preAllocatedArmorComponents,
                            __instance.Skills.LightVestMeleeWeaponDamageReduction,
                            __instance.Skills.HeavyVestBluntThroughputDamageReduction
                        );

                        __instance.method_96(armorDamageDealt, armor);
                    }

                    hasBlockedShot = (armor.Item.Id == damageInfo.BlockedBy);
                    hasDeflectedShot = (armor.Item.Id == damageInfo.DeflectedBy);
                }
            }

            if (armorDamageDealt > 0.1f)
            {
                __instance.OnArmorPointsChanged(armor, false);
            }
        }

        __result = hitArmorList;
        return false;
    }
}
