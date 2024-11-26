using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class MineDirectional_OnTriggerEnter_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MineDirectional).GetMethod(nameof(MineDirectional.OnTriggerEnter));
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
