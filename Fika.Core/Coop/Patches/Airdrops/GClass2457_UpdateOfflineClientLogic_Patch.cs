using Comfort.Common;
using Fika.Core.Coop.HostClasses;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass2457_UpdateOfflineClientLogic_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2457).GetMethod(nameof(GClass2457.UpdateOfflineClientLogic));
        }

        [PatchPostfix]
        public static void Postfix(AirplaneDataPacketStruct ___airplaneDataPacketStruct)
        {
            Singleton<CoopHostGameWorld>.Instance.FikaHostWorld.WorldPacket.SyncObjectPackets.Add(___airplaneDataPacketStruct);
        }
    }
}
