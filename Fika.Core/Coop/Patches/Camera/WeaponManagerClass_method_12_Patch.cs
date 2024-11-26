using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class WeaponManagerClass_method_12_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(WeaponManagerClass).GetMethod(nameof(WeaponManagerClass.method_12));
		}

		[PatchPostfix]
		public static void Postfix(WeaponManagerClass __instance, SightModVisualControllers[] ___sightModVisualControllers_0, Action<ESmoothScopeState> ___action_0, Action<float> ___action_1)
		{
			if (__instance.Player != null && !__instance.Player.IsYourPlayer)
			{
				foreach (SightModVisualControllers item in ___sightModVisualControllers_0)
				{
					if (item.TryGetZoomHandler(out ScopeZoomHandler scopeZoomHandler2))
					{
						scopeZoomHandler2.OnSmoothSensetivityChange -= ___action_1;
						scopeZoomHandler2.OnSmoothScopeStateChanged -= ___action_0;
						scopeZoomHandler2.SetUpdateEnable(false);
					}
				}
			}
		}
	}
}
