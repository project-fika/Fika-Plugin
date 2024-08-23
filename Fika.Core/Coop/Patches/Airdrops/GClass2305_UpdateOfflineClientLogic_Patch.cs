using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Airdrops
{
	public class GClass2305_UpdateOfflineClientLogic_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass2305).GetMethod(nameof(GClass2305.UpdateOfflineClientLogic));
		}

		[PatchPostfix]
		public static void Postfix(ref AirplaneDataPacketStruct ___airplaneDataPacketStruct)
		{

		}
	}
}
