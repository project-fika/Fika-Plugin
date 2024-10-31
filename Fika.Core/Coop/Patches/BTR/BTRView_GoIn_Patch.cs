using Comfort.Common;
using EFT;
using EFT.Vehicle;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Patches
{
	public class BTRView_GoIn_Patch : ModulePatch
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
				if (!isServer)
				{
					Singleton<GameWorld>.Instance.BtrController.TransferItemsController.InitPlayerStash(player);
				}
				__result = ObservedGoIn(__instance, observedPlayer, side, placeId, fast);
				return false;
			}

			if (player.IsYourPlayer)
			{
				CoopPlayer myPlayer = (CoopPlayer)player;
				myPlayer.PacketSender.Enabled = false;
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
				else
				{
					Singleton<GameWorld>.Instance.BtrController.TransferItemsController.InitPlayerStash(player);
				}
			}

			return true;
		}

		private static async Task ObservedGoIn(BTRView view, ObservedCoopPlayer observedPlayer, BTRSide side, byte placeId, bool fast)
		{
			try
			{
				CancellationToken cancellationToken = view.method_11(observedPlayer);
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
				view.method_17(observedPlayer);
				observedPlayer.CharacterController.isEnabled = false;
				observedPlayer.BtrState = EPlayerBtrState.GoIn;
				side.AddPassenger(observedPlayer, placeId);
				await view.method_14(observedPlayer.MovementContext.PlayerAnimator, fast, false, cancellationToken);
				if (!cancellationToken.IsCancellationRequested)
				{
					if (view.method_19() == 1)
					{
						GlobalEventHandlerClass.CreateEvent<GClass3260>().Invoke(observedPlayer.Side);
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
