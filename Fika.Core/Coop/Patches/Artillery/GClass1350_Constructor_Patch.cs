using Comfort.Common;
using Fika.Core.Coop.GameMode;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class GClass1350_Constructor_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass1375).GetConstructors().Single();
		}

		[PatchPrefix]
		public static bool Prefix(GClass1375 __instance, ref GClass1374 ___gclass1374_0)
		{
			__instance.speed = 50f;
			__instance.arcHeight = -150f;
			__instance.explosionDistnaceRange = new(3f, 5f);
			__instance.zoneID = "";
			CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
			coopGame.UpdateByUnity += __instance.OnUpdate;
			___gclass1374_0 = new();
			return false;
		}
	}
}
