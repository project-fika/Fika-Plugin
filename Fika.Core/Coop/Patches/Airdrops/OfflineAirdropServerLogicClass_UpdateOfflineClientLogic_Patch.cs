using Comfort.Common;
using Fika.Core.Coop.HostClasses;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class OfflineAirdropServerLogicClass_UpdateOfflineClientLogic_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(OfflineAirdropServerLogicClass).GetMethod(nameof(OfflineAirdropServerLogicClass.UpdateOfflineClientLogic));
        }

        [PatchPostfix]
        public static void Postfix(AirplaneDataPacketStruct ___AirplaneDataPacketStruct)
        {
            Singleton<CoopHostGameWorld>.Instance.FikaHostWorld.WorldPacket.SyncObjectPackets.Add(___AirplaneDataPacketStruct);
        }
    }
}
