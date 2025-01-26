using Comfort.Common;
using Fika.Core.Coop.HostClasses;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass2455_UpdateOfflineClientLogic_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2455).GetMethod(nameof(GClass2455.UpdateOfflineClientLogic));
        }

        [PatchPostfix]
        public static void Postfix(AirplaneDataPacketStruct ___airplaneDataPacketStruct)
        {
            Singleton<CoopHostGameWorld>.Instance.FikaHostWorld.SyncObjectPacket.Packets.Add(___airplaneDataPacketStruct);
        }
    }
}
