using Comfort.Common;
using EFT.BufferZone;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Lighthouse;

public class BufferInnerZone_ChangePlayerAccessStatus_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BufferInnerZone)
            .GetMethod(nameof(BufferInnerZone.ChangePlayerAccessStatus));
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

        Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
}
