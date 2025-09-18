using Comfort.Common;
using EFT.BufferZone;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Lighthouse;

public class BufferInnerZone_ChangeZoneInteractionAvailability_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BufferInnerZone)
            .GetMethod(nameof(BufferInnerZone.ChangeZoneInteractionAvailability));
    }

    [PatchPostfix]
    public static void Postfix(bool isAvailable, EBufferZoneData changesDataType)
    {
        if (FikaBackendUtils.IsClient)
        {
            return;
        }

        BufferZonePacket packet = new(changesDataType)
        {
            Available = isAvailable
        };

        Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
}
