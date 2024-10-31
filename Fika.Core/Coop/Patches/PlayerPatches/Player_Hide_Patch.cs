using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class Player_Hide_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(LocalPlayer).GetMethod(nameof(LocalPlayer.Hide));
		}

		// Check for GClass increments
		[PatchPrefix]
		public static bool Prefix(GClass886 ___gclass886_0)
		{
			___gclass886_0.Hide();
			return false;
		}
	}
}
