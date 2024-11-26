using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	/// <summary>
	/// This allows all game editions to edit the <see cref="EFT.RaidSettings"/>
	/// </summary>
	public class MainMenuController_method_49_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MainMenuController).GetMethod(nameof(MainMenuController.method_49));
		}

		[PatchPrefix]
		public static bool Prefix(ref bool __result)
		{
			__result = true;
			return false;
		}
	}
}
