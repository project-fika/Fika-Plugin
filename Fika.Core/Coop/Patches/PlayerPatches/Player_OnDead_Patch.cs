using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// This patch stops BSGs dogtag handling as it is poorly executed
	/// </summary>
	public class Player_OnDead_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			//Check for gclass increments
			return typeof(LocalPlayer).GetMethod(nameof(LocalPlayer.OnDead));
		}

		[PatchTranspiler]
		public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
		{
			// Create a new set of instructions
			List<CodeInstruction> instructionsList =
			[
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Call, typeof(Player).GetMethod(nameof(Player.OnDead))),
				new CodeInstruction(OpCodes.Ret)
			];

			return instructionsList;
		}
	}
}
