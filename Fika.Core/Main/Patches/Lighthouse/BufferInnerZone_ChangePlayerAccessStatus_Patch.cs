using Comfort.Common;
using EFT.BufferZone;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Patching;
using LiteNetLib;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    public class BufferInnerZone_ChangePlayerAccessStatus_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BufferInnerZone).GetMethod(nameof(BufferInnerZone.ChangePlayerAccessStatus));
        }

        [PatchPostfix]
        public static void Postfix(string profileID, bool status)
        {
            if (FikaBackendUtils.IsClient)
            {
                return;
            }

            BufferZonePacket packet = new(EBufferZoneData.PlayerAccessStatus)
            {
                ProfileId = profileID,
                Available = status
            };

            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }
    }
}
