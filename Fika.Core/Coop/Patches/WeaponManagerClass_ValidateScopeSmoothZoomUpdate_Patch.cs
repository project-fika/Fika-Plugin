using Fika.Core.Coop.Players;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class WeaponManagerClass_ValidateScopeSmoothZoomUpdate_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(WeaponManagerClass).GetMethod(nameof(WeaponManagerClass.ValidateScopeSmoothZoomUpdate));
		}

		[PatchPrefix]
		public static bool Prefix(WeaponManagerClass __instance)
		{
			if (__instance.Player is ObservedCoopPlayer)
			{
				return false;
			}
			return true;
		}
	}
}
