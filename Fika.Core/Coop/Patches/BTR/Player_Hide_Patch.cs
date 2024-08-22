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

		public static bool Prefix(GClass856 ___gclass856_0)
		{
			___gclass856_0.Hide();
			return false;
		}
	}
}
