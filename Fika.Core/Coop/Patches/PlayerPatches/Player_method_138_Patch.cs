using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.PlayerPatches
{
	/// <summary>
	/// This patch stops BSGs dogtag handling as it is poorly executed
	/// </summary>
	public class Player_method_138_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(LocalPlayer).GetMethod(nameof(LocalPlayer.method_138));
		}

		[PatchPrefix]
		public static bool Prefix()
		{
			return false;
		}
	}
}
