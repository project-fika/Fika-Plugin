using Comfort.Common;
using Fika.Core.Coop.HostClasses;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass2461_UpdateOfflineClientLogic_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2461).GetMethod(nameof(GClass2461.UpdateOfflineClientLogic));
        }

        [PatchPostfix]
        public static void Postfix(AirplaneDataPacketStruct ___airplaneDataPacketStruct)
        {
            Singleton<CoopHostGameWorld>.Instance.FikaHostWorld.WorldPacket.SyncObjectPackets.Add(___airplaneDataPacketStruct);
        }
    }
}
