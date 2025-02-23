using Comfort.Common;
using Fika.Core.Coop.HostClasses;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass2456_UpdateOfflineClientLogic_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2456).GetMethod(nameof(GClass2456.UpdateOfflineClientLogic));
        }

        [PatchPostfix]
        public static void Postfix(AirplaneDataPacketStruct ___airplaneDataPacketStruct)
        {
            Singleton<CoopHostGameWorld>.Instance.FikaHostWorld.WorldPacket.SyncObjectPackets.Add(___airplaneDataPacketStruct);
        }
    }
}
