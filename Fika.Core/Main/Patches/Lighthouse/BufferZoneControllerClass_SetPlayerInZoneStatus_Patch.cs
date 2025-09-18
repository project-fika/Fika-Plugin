using Comfort.Common;
using EFT.BufferZone;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Lighthouse;

public class BufferZoneControllerClass_SetPlayerInZoneStatus_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BufferZoneControllerClass)
            .GetMethod(nameof(BufferZoneControllerClass.SetPlayerInZoneStatus));
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

        Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
}
