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

public class BTRView_GoOut_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BTRView).GetMethod(nameof(BTRView.GoOut),
            [typeof(Player), typeof(BTRSide), typeof(byte), typeof(bool)]);
    }

    [PatchPrefix]
    public static bool Prefix(BTRView __instance, Player player, BTRSide side, bool fast, byte placeId, ref Il2CppSystem.Threading.Tasks.Task __result)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        if (player is ObservedPlayer observedPlayer)
        {
            __result = ObservedGoOut(__instance, observedPlayer, side, fast).ToIl2Cpp();
            FikaGlobals.NetworkManager.ObservedPlayers.Add(observedPlayer);
            return false;
        }

        if (player.IsYourPlayer)
        {
            var myPlayer = (FikaPlayer)player;
            myPlayer.PacketSender.SendState = true;
            if (FikaBackendUtils.IsServer)
            {
                BTRInteractionPacket packet = new(myPlayer.NetId)
                {
                    Data = new()
                    {
                        HasInteraction = true,
                        InteractionType = EInteractionType.GoOut,
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

    private static async Task ObservedGoOut(BTRView view, ObservedPlayer observedPlayer, BTRSide side, bool fast)
    {
        try
        {
            var cancellationToken = view.PlayerToken(observedPlayer);
            observedPlayer.BtrState = EPlayerBtrState.GoOut;
            var soundController = view._soundController;
            if (soundController != null)
            {
                soundController.UpdateBtrAudioRoom(EnvironmentType.Outdoor, observedPlayer);
            }
            await view.GoOutAnimation(observedPlayer.MovementContext.PlayerAnimator, fast, true, cancellationToken);
            var valueTuple = side.GoOutPoints();
            side.ApplyPlayerRotation(observedPlayer.MovementContext, valueTuple.Item1, valueTuple.Item2 + Vector3.up * 1.9f);
            observedPlayer.BtrState = EPlayerBtrState.Outside;
            observedPlayer.CharacterController.isEnabled = true;
            side.RemovePassenger(observedPlayer);
            observedPlayer.MovementContext.IsAxesIgnored = false;
            observedPlayer.IsInBufferZone = false;
        }
        catch (Exception ex)
        {
            FikaGlobals.LogError("BTRView_GoOut_Patch: " + ex.Message);
        }
    }
}
