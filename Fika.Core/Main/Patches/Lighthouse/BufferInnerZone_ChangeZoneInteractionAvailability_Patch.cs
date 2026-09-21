using System.Reflection;
using Comfort.Common;
using EFT.BufferZone;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using SPTushonka.Reflection.Patching;

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
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        if (FikaBackendUtils.IsClient)
        {
            return;
        }

        BufferZonePacket packet = new(changesDataType)
        {
            Available = isAvailable
        };

        FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
}
