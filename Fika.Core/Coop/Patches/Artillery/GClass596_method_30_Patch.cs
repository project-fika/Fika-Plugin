using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class GClass596_method_30_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass596).GetMethod(nameof(GClass596.method_30));
		}

		[PatchPrefix]
		public static void Prefix(GClass596 __instance, GClass1350 serverProjectile)
		{
			__instance.method_31(serverProjectile);
		}
	}
}
