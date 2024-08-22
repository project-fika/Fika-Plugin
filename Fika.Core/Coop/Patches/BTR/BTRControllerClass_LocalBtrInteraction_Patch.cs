using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.BTR
{
	public class BTRControllerClass_LocalBtrInteraction_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BTRControllerClass).GetMethod(nameof(BTRControllerClass.LocalBtrInteraction));
		}

		[PatchPrefix]
		public static bool Prefix()
		{
			if (FikaBackendUtils.IsClient)
			{

				return false;
			}
			return true;
		}
	}
}
