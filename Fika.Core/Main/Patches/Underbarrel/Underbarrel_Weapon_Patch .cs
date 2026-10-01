using System.Reflection;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Underbarrel;

/// <summary>
/// Backported from SPT 5.0 <br/>
/// Created by Archangel
/// </summary>
public sealed class Underbarrel_Weapon_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Weapon)
            .GetMethod(nameof(Weapon.GetUnderbarrelWeapon));
    }

    [PatchPrefix]
    public static bool PatchPrefix(Weapon __instance, ref Launcher __result)
    {
        __result = GetUnderbarrelWeapon(__instance);
        return false;
    }

    private static Launcher GetUnderbarrelWeapon(CompoundItem item)
    {
        var slots = item?.Slots;
        if (slots == null)
        {
            return null;
        }

        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot != null && slot.ContainedItem is Launcher launcher)
            {
                return launcher;
            }
        }

        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot != null && slot.ContainedItem is CompoundItem childCompound)
            {
                var found = GetUnderbarrelWeapon(childCompound);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }
}
