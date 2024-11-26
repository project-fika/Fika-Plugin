using EFT.UI;
using Fika.Core.UI.Custom;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class MenuScreen_Awake_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MenuScreen).GetMethod(nameof(MenuScreen.Awake));
		}

		[PatchPostfix]
		public static void Postfix(MenuScreen __instance)
		{
			__instance.gameObject.AddComponent<MainMenuUIScript>();
		}
	}
}
