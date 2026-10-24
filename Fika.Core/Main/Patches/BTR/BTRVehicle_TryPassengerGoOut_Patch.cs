using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Vehicle;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.BTR;

internal class BTRVehicle_TryPassengerGoOut_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BTRVehicle), nameof(BTRVehicle.ProcessGoOutList));
    }

    [PatchPrefix]
    public static bool Prefix(BTRVehicle __instance)
    {
        if (!FikaBackendUtils.IsServer)
        {
            return true;
        }

        var passengers = __instance._passengers;
        for (var i = 0; i < passengers.Count; i++)
        {
            var passenger = passengers[i];
            if (__instance.IsInBlackList(passenger.Player))
            {
                __instance.AddToGoOutList(passenger);
            }
        }

        var goOutList = __instance._goOutList;
        for (var i = 0; i < goOutList.Count; i++)
        {
            var passenger = goOutList[i];
            var status = __instance.InteractInternal(passenger.Player, new InteractWithBtrPacket
            {
                HasInteraction = true,
                InteractionType = EInteractionType.GoOut,
                SideId = passenger.SideId,
                SlotId = passenger.SlotId
            });

            if (status is EBtrInteractionStatus.Confirmed or EBtrInteractionStatus.EmptySlot)
            {
                goOutList.RemoveAt(i);
                if (passenger.Player is ObservedPlayer observedPlayer)
                {
                    BTRInteractionPacket packet = new(observedPlayer.NetId)
                    {
                        IsResponse = true,
                        Status = status,
                        Data = new()
                        {
                            HasInteraction = true,
                            InteractionType = EInteractionType.GoOut,
                            SideId = passenger.SideId,
                            SlotId = passenger.SlotId,
                            Fast = false
                        }
                    };

                    FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        return false;
    }
}
