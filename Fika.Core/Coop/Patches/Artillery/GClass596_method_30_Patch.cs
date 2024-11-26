using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class GClass607_method_30_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass607).GetMethod(nameof(GClass607.method_30));
		}

		[PatchPrefix]
		public static void Prefix(GClass607 __instance, GClass1375 serverProjectile)
		{
			__instance.method_31(serverProjectile);
		}
	}
}
