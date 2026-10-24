using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using SPT.Reflection.Patching;

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
        Singleton<IFikaNetworkManager>.Instance.SendGenericPacket(in packet, DeliveryMethod.ReliableOrdered, true);
    }
}
