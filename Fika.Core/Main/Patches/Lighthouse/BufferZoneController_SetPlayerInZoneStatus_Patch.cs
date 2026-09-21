using System.Reflection;
using Comfort.Common;
using EFT.BufferZone;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Lighthouse;

public class BufferZoneController_SetPlayerInZoneStatus_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BufferZoneController),
            nameof(BufferZoneController.OnPlayerInZoneStatusChanged));
    }

    [PatchPostfix]
    public static void Postfix(int playerRaidID, bool inZone)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        if (FikaBackendUtils.IsClient)
        {
            return;
        }

        BufferZonePacket packet = new(EBufferZoneData.PlayerInZoneStatusChange)
        {
            PlayerRaidId = playerRaidID,
            Available = inZone
        };

        FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
}
