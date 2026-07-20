using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Tripwire;

public class TripwireSynchronizableObject_ActivateGrenade_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TripwireSynchronizableObject)
            .GetMethod(nameof(TripwireSynchronizableObject.ActivateGrenade));
    }

    [PatchPostfix]
    public static void Prefix(Grenade ____grenadeInWorld)
    {
        Singleton<IGameLevel>.Instance.RegisterGrenade(____grenadeInWorld);
    }
}
