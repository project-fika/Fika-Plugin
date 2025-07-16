using Comfort.Common;
using EFT.BufferZone;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Patching;
using LiteNetLib;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    public class BufferZoneControllerClass_SetPlayerInZoneStatus_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BufferZoneControllerClass).GetMethod(nameof(BufferZoneControllerClass.SetPlayerInZoneStatus));
        }

        [PatchPostfix]
        public static void Postfix(string profileID, bool inZone)
        {
            if (FikaBackendUtils.IsClient)
            {
                return;
            }

            BufferZonePacket packet = new(EBufferZoneData.PlayerInZoneStatusChange)
            {
                ProfileId = profileID,
                Available = inZone
            };

            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }
    }
}
