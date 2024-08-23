using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Tripwire
{
	internal class TripwireSynchronizableObject_method_6_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(TripwireSynchronizableObject).GetMethod(nameof(TripwireSynchronizableObject.method_6));
		}

		[PatchPostfix]
		public static void Prefix(Grenade ____grenadeInWorld)
		{
			Singleton<GInterface121>.Instance.RegisterGrenade(____grenadeInWorld);
		}
	}
}
