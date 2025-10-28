using Comfort.Common;
using EFT.SynchronizableObjects;
using Fika.Core.Main.HostClasses;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Tripwire;

internal class TripwireSynchronizableObject_method_11_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TripwireSynchronizableObject).GetMethod(nameof(TripwireSynchronizableObject.method_11));
    }

    [PatchPrefix]
    public static void Prefix(TripwireSynchronizableObject __instance)
    {
        if (FikaBackendUtils.IsServer)
        {
            AirplaneDataPacketStruct packet = new()
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
