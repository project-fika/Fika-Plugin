using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.ArmorSystem;

/// <summary>
/// This skips recalculation of armors since Fika caches them
/// </summary>
internal class Player_TryGetArmorResistData_Transpiler : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player),
            nameof(Player.TryGetArmorResistData));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        codes.RemoveRange(2, 8);
        return codes;
    }
}
