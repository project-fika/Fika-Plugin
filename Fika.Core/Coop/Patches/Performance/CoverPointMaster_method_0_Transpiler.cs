using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Coop.Patches
{
	internal class CoverPointMaster_method_0_Transpiler : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(CoverPointMaster).GetMethod(nameof(CoverPointMaster.method_0));
		}

		[PatchTranspiler]
		public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codeList = instructions.ToList();

			codeList[69].opcode = OpCodes.Nop;
			codeList[12].opcode = OpCodes.Nop;
			codeList[11].opcode = OpCodes.Nop;
			codeList[10].opcode = OpCodes.Nop;

			return codeList;
		}
	}
}
