using Comfort.Common;
using Fika.Core.Main.HostClasses;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    public class OfflineAirplaneServerLogicClass_UpdateOfflineClientLogic_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(OfflineAirplaneServerLogicClass).GetMethod(nameof(OfflineAirplaneServerLogicClass.UpdateOfflineClientLogic));
        }

        [PatchPostfix]
        public static void Postfix(AirplaneDataPacketStruct ___AirplaneDataPacketStruct)
        {
            Singleton<CoopHostGameWorld>.Instance.FikaHostWorld.WorldPacket.SyncObjectPackets.Add(___AirplaneDataPacketStruct);
        }
    }
}
