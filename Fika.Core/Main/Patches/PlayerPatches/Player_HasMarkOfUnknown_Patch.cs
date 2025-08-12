using EFT;
using EFT.InventoryLogic;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.PlayerPatches;

class Player_HasMarkOfUnknown_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.HasMarkOfUnknown));
    }

    [PatchPrefix]
    public static bool Prefix(Player __instance, ref MarkOfUnknownItemClass markOfUnknown, ref bool __result)
    {
        __result = false;
        CompoundItem compoundItem = __instance.InventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Pockets).ContainedItem as PocketsItemClass;
        if (compoundItem != null)
        {
            markOfUnknown = null;
            if (compoundItem.Slots != null)
            {
                Slot[] slots = compoundItem.Slots;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].ContainedItem is MarkOfUnknownItemClass markOfUnknownItemClass)
                    {
                        markOfUnknown = markOfUnknownItemClass;
                        __result = true;
                    }
                }
            }
        }

        return false;
    }
}
