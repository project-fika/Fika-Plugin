using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// This patch stops BSGs dogtag handling as it is poorly executed
	/// </summary>
	public class Player_SetDogtagInfo_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(Player).GetMethod(nameof(Player.SetDogtagInfo));
		}

		[PatchPrefix]
		public static bool Prefix()
		{
			return false;
		}
	}
}
