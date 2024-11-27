using EFT.UI;
using Fika.Core.UI.Custom;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class MenuScreen_method_8_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MenuScreen).GetMethod(nameof(MenuScreen.method_8));
		}

		[PatchPostfix]
		public static void Postfix(bool minimized)
		{
			if (!minimized && MainMenuUIScript.Exist)
			{
				MainMenuUIScript.Instance.UpdatePresence(FikaUIGlobals.EFikaPlayerPresence.IN_MENU);
			}
		}
	}
}
