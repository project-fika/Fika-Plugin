using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class GClass3224_IsValid_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass3224).GetMethods().FirstOrDefault(x => x.Name == "IsValid" && x.GetParameters().Length == 4);
		}

		[PatchPrefix]
		public static void Prefix(ref float distanceSqr)
		{
			distanceSqr = 500f;
		}
	}
}
