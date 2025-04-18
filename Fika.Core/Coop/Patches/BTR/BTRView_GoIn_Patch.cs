using Audio.Vehicles.BTR;
using Comfort.Common;
using EFT;
using EFT.Vehicle;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Patching;
using HarmonyLib;
using LiteNetLib;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Patches
{
    public class BTRView_GoIn_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BTRView).GetMethod(nameof(BTRView.GoIn));
        }

        [PatchPrefix]
        public static bool Prefix(BTRView __instance, Player player, BTRSide side, byte placeId, bool fast, ref Task __result)
        {
            bool isServer = FikaBackendUtils.IsServer;
            if (player is ObservedCoopPlayer observedPlayer)
            {
                __result = ObservedGoIn(__instance, observedPlayer, side, placeId, fast);
                Singleton<IFikaNetworkManager>.Instance.ObservedCoopPlayers.Remove(observedPlayer);
                return false;
            }

            if (player.IsYourPlayer)
            {
                CoopPlayer myPlayer = (CoopPlayer)player;
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

                    Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
                }
            }

            return true;
        }

        private static async Task ObservedGoIn(BTRView view, ObservedCoopPlayer observedPlayer, BTRSide side, byte placeId, bool fast)
        {
            try
            {
                CancellationToken cancellationToken = view.method_12(observedPlayer);
                observedPlayer.MovementContext.IsAxesIgnored = true;
                observedPlayer.BtrState = EPlayerBtrState.Approach;
                if (!fast)
                {
                    ValueTuple<Vector3, Vector3> valueTuple = side.GoInPoints();
                    await side.ProcessApproach(observedPlayer, valueTuple.Item1, valueTuple.Item2 + Vector3.up * 1.4f);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
                view.method_18(observedPlayer);
                observedPlayer.CharacterController.isEnabled = false;
                observedPlayer.BtrState = EPlayerBtrState.GoIn;
                side.AddPassenger(observedPlayer, placeId);
                BtrSoundController soundController = Traverse.Create(view).Field<BtrSoundController>("_soundController").Value;
                if (soundController != null)
                {
                    soundController.UpdateBtrAudioRoom(EnvironmentType.Indoor, observedPlayer);
                }
                await view.method_15(observedPlayer.MovementContext.PlayerAnimator, fast, true, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (view.method_20() == 1)
                    {
                        GlobalEventHandlerClass.CreateEvent<GClass3330>().Invoke(observedPlayer.Side);
                    }
                    observedPlayer.BtrState = EPlayerBtrState.Inside;
                }
            }
            catch (Exception ex)
            {
                FikaPlugin.Instance.FikaLogger.LogError("BTRView_GoIn_Patch: " + ex.Message);
            }
        }
    }
}
