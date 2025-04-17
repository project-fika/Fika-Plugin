using Dissonance.Audio.Capture;
using HarmonyLib;
using Fika.Core.Patching;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Coop.Patches.VOIP
{
    /// <summary>
    /// Used to temporarily mitigate a bug that causes log spam until the bug can be resolved
    /// </summary>
    class BasicMicrophoneCapture_UpdateSubscribers_Transpiler : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BasicMicrophoneCapture).GetMethod(nameof(BasicMicrophoneCapture.UpdateSubscribers));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            // Create a new set of instructions
            List<CodeInstruction> instructionsList = [.. instructions];
            instructionsList[23].opcode = OpCodes.Nop;
            instructionsList[24].opcode = OpCodes.Nop;
            instructionsList[25].opcode = OpCodes.Nop;

            return instructionsList;
        }
    }
}
