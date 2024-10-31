using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// Used to speed up debugging
	/// </summary>
	public class GClass1640_method_0_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass1640).GetMethod(nameof(GClass1640.method_0));
		}

		[PatchPrefix]
		public static void Prefix(ref LocationSettingsClass.Location.TransitParameters[] parameters)
		{
			foreach (LocationSettingsClass.Location.TransitParameters parameter in parameters)
			{
				parameter.activateAfterSec = 10;
			}
		}
	}
}
