using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class AIPlaceLogicPartisan_Dispose_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(AIPlaceLogicPartisan).GetMethod(nameof(AIPlaceLogicPartisan.Dispose));
		}

		[PatchPrefix]
		public static bool Prefix(AIPlaceInfo ___aiplaceInfo_0)
		{
			if (___aiplaceInfo_0 == null)
			{
				return false;
			}

			return true;
		}
	}
}
