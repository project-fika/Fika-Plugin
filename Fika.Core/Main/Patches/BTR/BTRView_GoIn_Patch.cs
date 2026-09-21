using EFT.GlobalEvents;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Audio.Vehicles.BTR;
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

public class BTRView_GoIn_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BTRView).GetMethod(nameof(BTRView.GoIn),
            [typeof(Player), typeof(BTRSide), typeof(byte), typeof(bool)]);
    }

    [PatchPrefix]
    public static bool Prefix(BTRView __instance, Player player, BTRSide side, byte placeId, bool fast, ref Il2CppSystem.Threading.Tasks.Task __result)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        var isServer = FikaBackendUtils.IsServer;
        if (player is ObservedPlayer observedPlayer)
        {
            __result = ObservedGoIn(__instance, observedPlayer, side, placeId, fast).ToIl2Cpp();
            FikaGlobals.NetworkManager.ObservedPlayers.Remove(observedPlayer);
            return false;
        }

        if (player.IsYourPlayer)
        {
            var myPlayer = (FikaPlayer)player;
            myPlayer.PacketSender.SendState = false;
            player.InputDirection = new(0, 0);
            if (isServer)
            {
                BTRInteractionPacket packet = new(myPlayer.NetId)
                {
                    Data = new()
                    {
                        HasInteraction = true,
                        InteractionType = EInteractionType.GoIn,
                        SideId = __instance.GetSideId(side),
                        SlotId = placeId,
                        Fast = fast
                    }
                };

                FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        return true;
    }

    private static async Task ObservedGoIn(BTRView view, ObservedPlayer observedPlayer, BTRSide side, byte placeId, bool fast)
    {
        try
        {
            var cancellationToken = view.PlayerToken(observedPlayer);
            observedPlayer.MovementContext.IsAxesIgnored = true;
            observedPlayer.BtrState = EPlayerBtrState.Approach;
            if (!fast)
            {
                var valueTuple = side.GoInPoints();
                await side.ProcessApproach(observedPlayer, valueTuple.Item1, valueTuple.Item2 + Vector3.up * 1.4f);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
            observedPlayer.HideWeapon();
            observedPlayer.CharacterController.isEnabled = false;
            observedPlayer.BtrState = EPlayerBtrState.GoIn;
            side.AddPassenger(observedPlayer, placeId);
            var soundController = view._soundController;
            if (soundController != null)
            {
                soundController.UpdateBtrAudioRoom(EnvironmentType.Indoor, observedPlayer);
            }
            await view.GoInAnimation(observedPlayer.MovementContext.PlayerAnimator, fast, true, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                if (view.GetBusyPlacesCount() == 1)
                {
                    GlobalEventsController.CreateEvent<BtrFirstPassengerGoInEvent>().Invoke(observedPlayer.Side);
                }
                observedPlayer.BtrState = EPlayerBtrState.Inside;
            }
        }
        catch (Exception ex)
        {
            FikaGlobals.LogError("BTRView_GoIn_Patch: " + ex.Message);
        }
    }
}
