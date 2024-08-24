using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.BTR
{
	public class Player_Hide_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(LocalPlayer).GetMethod(nameof(LocalPlayer.Hide));
		}

		// Check for GClass increments
		[PatchPrefix]
		public static bool Prefix(GClass857 ___gclass857_0)
		{
			___gclass857_0.Hide();
			return false;
		}
	}
}
