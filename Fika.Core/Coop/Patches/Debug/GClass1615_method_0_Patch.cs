using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// Used to speed up debugging
	/// </summary>
	public class GClass1615_method_0_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass1615).GetMethod(nameof(GClass1615.method_0));
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
