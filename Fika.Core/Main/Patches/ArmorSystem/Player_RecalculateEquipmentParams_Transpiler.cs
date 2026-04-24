using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.ArmorSystem;

/// <summary>
/// This skips recalculation of armors since Fika caches them
/// </summary>
internal class Player_RecalculateEquipmentParams_Transpiler : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player),
            nameof(Player.RecalculateEquipmentParams));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        FikaGlobals.MigrateLabels(codes, 148, 3);
        codes.RemoveRange(148, 3);

        FikaGlobals.MigrateLabels(codes, 7, 8);
        codes.RemoveRange(7, 8);
        return codes;
    }
}
