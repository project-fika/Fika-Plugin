using EFT.UI.Ragfair;
using Fika.Core.UI.Custom;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class RagfairScreen_Show_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(RagfairScreen).GetMethod(nameof(RagfairScreen.Show));
		}

		[PatchPostfix]
		public static void Postfix()
		{
			if (MainMenuUIScript.Exist)
			{
				MainMenuUIScript.Instance.UpdatePresence(FikaUIGlobals.EFikaPlayerPresence.IN_FLEA);
			}
		}
	}
}
