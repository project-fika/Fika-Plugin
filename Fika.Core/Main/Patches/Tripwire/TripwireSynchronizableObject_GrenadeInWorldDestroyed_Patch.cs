using System.Reflection;
using Comfort.Common;
using EFT.SynchronizableObjects;
using Fika.Core.Main.HostClasses;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Tripwire;

internal class TripwireSynchronizableObject_GrenadeInWorldDestroyed_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TripwireSynchronizableObject).GetMethod(nameof(TripwireSynchronizableObject.GrenadeInWorldDestroyed));
    }

    [PatchPrefix]
    public static void Prefix(TripwireSynchronizableObject __instance)
    {
        if (FikaBackendUtils.IsServer)
        {
            SynchronizableObjectPacket packet = new()
            {
                ObjectType = SynchronizableObjectType.Tripwire,
                ObjectId = __instance.ObjectId,
                PacketData = new()
                {
                    TripwireDataPacket = new()
                    {
                        State = ETripwireState.Exploded
                    }
                },
                Position = __instance.transform.position,
                Rotation = __instance.transform.rotation.eulerAngles,
                IsActive = true
            };

            var hostWorld = Singleton<FikaHostGameWorld>.Instance.FikaHostWorld;
            hostWorld.WorldPacket.SyncObjectPackets.Add(packet);
            hostWorld.SetCritical();
        }
    }
}
