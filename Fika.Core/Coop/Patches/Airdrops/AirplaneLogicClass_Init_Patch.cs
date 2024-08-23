using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Airdrops
{
	internal class AirplaneLogicClass_Init_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(AirplaneLogicClass).GetMethod(nameof(AirplaneLogicClass.Init));
		}

		[PatchPrefix]
		public static void Prefix(ref bool ___offlineMode, ref GClass2305 ___OfflineServerLogic)
		{
			if (FikaBackendUtils.IsClient)
			{
				___offlineMode = false;
				___OfflineServerLogic = null;
			}
		}
	}
}
