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
internal class Player_IsShotDeflectedByHeavyArmor_Transpiler : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player),
            nameof(Player.IsShotDeflectedByHeavyArmor));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        codes.RemoveRange(11, 8);
        return codes;
    }
}
