using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	internal class GClass2013_method_0_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass2013).GetMethod(nameof(GClass2013.method_0));
		}

		[PatchPrefix]
		public static void Prefix(ref GStruct238 preset)
		{
			if (FikaBackendUtils.IsClient)
			{
				Logger.LogInfo("Disabling server scenes");
				preset.DisableServerScenes = true;
			}
		}
	}
}
