using Comfort.Common;
using EFT;
using EFT.Vehicle;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	internal class BTRVehicle_method_38_Patch : ModulePatch
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
					if (passenger.Player is ObservedCoopPlayer observedPlayer)
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

						Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
					}
				}
			}
		}
	}
}
