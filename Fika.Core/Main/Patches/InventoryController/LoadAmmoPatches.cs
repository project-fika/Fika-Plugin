using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using EFT;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.InventoryController;

[IgnoreAutoPatch]
public sealed class LoadAmmo_Task_Transpiler : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player.PlayerInventoryController.Class1204.Struct307),
            nameof(Player.PlayerInventoryController.Class1204.Struct307.MoveNext));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var array = instructions.ToArray();
        array[76].opcode = OpCodes.Ldc_I4_5; // load 5 bullets instead of 1
        array[138].opcode = OpCodes.Ldc_I4_5; // load 5 bullets instead of 1
        return array;
    }
}

[IgnoreAutoPatch]
public sealed class ItemViewLoadAmmoComponent_Show_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ItemViewLoadAmmoComponent),
            nameof(ItemViewLoadAmmoComponent.Show));
    }

    [PatchPrefix]
    public static void Prefix(ref int ammoTotal)
    {
        ammoTotal = Mathf.CeilToInt(ammoTotal / 5f);
    }
}
