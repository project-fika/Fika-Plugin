using EFT;
using Fika.Core.UI.Custom;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class HideoutPlayerOwner_SetPointOfView_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(HideoutPlayerOwner).GetMethod(nameof(HideoutPlayerOwner.SetPointOfView));
		}

		[PatchPostfix]
		public static void Postfix(HideoutPlayerOwner __instance)
		{
			if (__instance.FirstPersonMode && MainMenuUIScript.Exist)
			{
				MainMenuUIScript.Instance.UpdatePresence(FikaUIGlobals.EFikaPlayerPresence.IN_HIDEOUT);
			}
		}
	}
}
