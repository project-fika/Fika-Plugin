using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class MatchmakerPlayerControllerClass_GetCoopBlockReason_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MatchmakerPlayerControllerClass).GetMethod(nameof(MatchmakerPlayerControllerClass.GetCoopBlockReason));
		}

		[PatchPrefix]
		public static bool Prefix(ref ECoopBlock __result)
		{
			__result = ECoopBlock.NoBlock;
			return false;
		}
	}
}
