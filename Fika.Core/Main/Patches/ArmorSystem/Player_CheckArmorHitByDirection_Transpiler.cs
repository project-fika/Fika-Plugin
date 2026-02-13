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
internal class Player_CheckArmorHitByDirection_Transpiler : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player),
            nameof(Player.CheckArmorHitByDirection));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        return instructions.Skip(8);
    }
}
