using System.Reflection;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Lighthouse;

public class RadioTransmitterHandler_SetEncoded_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RadioTransmitterHandler).GetMethod(nameof(RadioTransmitterHandler.SetEncoded));
    }

    [PatchPostfix]
    public static void Postfix(RadioTransmitterHandler __instance, bool isEncoded)
    {
        if (!FikaBackendUtils.IsServer)
        {
            return;
        }

        if (__instance._player is not ObservedPlayer observedPlayer || observedPlayer.IsObservedAI)
        {
            return;
        }

        var packet = new TransmitterEncodedPacket(observedPlayer.NetId, isEncoded);
        FikaGlobals.NetworkManager.SendGenericPacket(in packet, DeliveryMethod.ReliableOrdered, true);
    }
}
