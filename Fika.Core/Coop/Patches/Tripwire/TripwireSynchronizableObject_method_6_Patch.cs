using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    internal class TripwireSynchronizableObject_method_6_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TripwireSynchronizableObject).GetMethod(nameof(TripwireSynchronizableObject.method_6));
        }

        [PatchPostfix]
        public static void Prefix(Grenade ____grenadeInWorld)
        {
            Singleton<GInterface151>.Instance.RegisterGrenade(____grenadeInWorld);
        }
    }
}
