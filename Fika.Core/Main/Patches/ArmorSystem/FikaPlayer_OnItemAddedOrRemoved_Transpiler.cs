using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.ArmorSystem;

/// <summary>
/// This skips recalculation of armors since Fika caches them
/// </summary>
internal class FikaPlayer_OnItemAddedOrRemoved_Transpiler : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(FikaPlayer),
            nameof(FikaPlayer.OnItemAddedOrRemoved));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        FikaGlobals.MigrateLabels(codes, 83, 25);
        codes.RemoveRange(83, 25);
        return codes;
    }
}
