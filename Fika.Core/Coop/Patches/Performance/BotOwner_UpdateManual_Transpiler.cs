using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Coop.Patches
{
	internal class BotOwner_UpdateManual_Transpiler : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BotOwner).GetMethod(nameof(BotOwner.UpdateManual));
		}

		[PatchTranspiler]
		public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codeList = instructions.ToList();

			codeList[109] = new CodeInstruction(OpCodes.Nop);
			codeList[108] = new CodeInstruction(OpCodes.Nop);
			codeList[107].opcode = OpCodes.Nop;
			codeList[18] = new CodeInstruction(OpCodes.Nop);
			codeList[14] = new CodeInstruction(OpCodes.Nop);
			codeList[13] = new CodeInstruction(OpCodes.Nop);
			codeList[12] = new CodeInstruction(OpCodes.Nop);

			return codeList;
		}
	}
}
