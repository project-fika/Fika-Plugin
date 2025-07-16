using Comfort.Common;
using EFT.SynchronizableObjects;
using Fika.Core.Main.HostClasses;
using Fika.Core.Main.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    internal class TripwireSynchronizableObject_method_11_Patch : FikaPatch
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
                Singleton<CoopHostGameWorld>.Instance.FikaHostWorld.WorldPacket.SyncObjectPackets.Add(packet);
            }
        }
    }
}
