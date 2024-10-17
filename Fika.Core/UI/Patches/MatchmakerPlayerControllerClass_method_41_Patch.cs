using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class MatchmakerPlayerControllerClass_method_41_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MatchmakerPlayerControllerClass).GetMethod(nameof(MatchmakerPlayerControllerClass.method_41));
		}

		[PatchPrefix]
		public static bool Prefix(ref bool __result)
		{
			__result = true;
			return false;
		}
	}
}
