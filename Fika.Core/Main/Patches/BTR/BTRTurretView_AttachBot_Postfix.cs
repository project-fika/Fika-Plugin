using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using EFT.Vehicle;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.BTR;

/// <summary>
/// Makes sure that the BTRShooter is updated on clients so that it moves with the BTR
/// </summary>
public class BTRTurretView_AttachBot_Postfix : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BTRTurretView)
            .GetMethod(nameof(BTRTurretView.AttachBot));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var il = instructions.ToList();
        il[92].operand = typeof(FikaBackendUtils)
            .GetProperty(nameof(FikaBackendUtils.IsClient))
            .GetGetMethod();

        var nopIntr = il[93];
        nopIntr.opcode = OpCodes.Nop;
        nopIntr.operand = null;

        return il;
    }
}
