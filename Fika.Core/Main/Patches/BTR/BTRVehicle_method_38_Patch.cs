using Comfort.Common;
using EFT;
using EFT.Vehicle;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.BTR;

internal class BTRVehicle_method_38_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BTRVehicle).GetMethod(nameof(BTRVehicle.method_38));
    }

    [PatchPostfix]
    public static void Postfix(BTRPassenger passenger, EBtrInteractionStatus __result)
    {
        if (FikaBackendUtils.IsServer)
        {
            if (__result is EBtrInteractionStatus.Confirmed or EBtrInteractionStatus.EmptySlot)
            {
                if (passenger.Player is ObservedPlayer observedPlayer)
                {
                    BTRInteractionPacket packet = new(observedPlayer.NetId)
                    {
                        IsResponse = true,
                        Status = __result,
                        Data = new()
                        {
                            HasInteraction = true,
                            InteractionType = EInteractionType.GoOut,
                            SideId = passenger.SideId,
                            SlotId = passenger.SlotId,
                            Fast = false
                        }
                    };

                    Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
                }
            }
        }
    }
}
