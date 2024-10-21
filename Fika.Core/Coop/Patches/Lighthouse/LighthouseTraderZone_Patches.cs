using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches
{
	class LighthouseTraderZone_Patches
	{
		public static void Enable()
		{
			new LighthouseTraderZone_AddPlayer_Patch().Enable();
			new LighthouseTraderZone_RemovePlayer_Patch().Enable();
			new LighthouseTraderZone_Awake_Patch().Enable();
		}

		internal class LighthouseTraderZone_AddPlayer_Patch : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				return typeof(LighthouseTraderZone).GetMethod(nameof(LighthouseTraderZone.AddPlayer));
			}

			[PatchPrefix]
			public static bool Prefix(Player player, LighthouseTraderZone __instance, ref List<Player> ___allPlayersInZone,
				ref List<Player> ___allowedPlayers, ref List<Player> ___unallowedPlayers, ref Action<string, bool> ___action_0)
			{
				if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
				{
					return false;
				}

				player.OnPlayerDead += __instance.method_7;
				bool flag = player.RecodableItemsHandler.TryToGetRecodableComponent(out RadioTransmitterRecodableComponent radioTransmitterRecodableComponent);

				if (player.IsAI && __instance.method_3(player))
				{
					___allowedPlayers.Add(player);
					___allPlayersInZone.Add(player);

					if (coopHandler.MyPlayer == player)
					{
						//Todo: Might have to patch method_6 as the host and other clients might need to know IsAgressorInLighthouseTraderZone
						player.ActiveHealthController.OnApplyDamageByPlayer += __instance.method_6;
					}

					return false;
				}
				if (!flag)
				{
					___unallowedPlayers.Add(player);
					___allPlayersInZone.Add(player);
					return false;
				}
				if (!__instance.method_4(radioTransmitterRecodableComponent.Handler))
				{
					___unallowedPlayers.Add(player);
					___action_0?.Invoke(player.ProfileId, false);
				}
				else
				{
					___allowedPlayers.Add(player);
					radioTransmitterRecodableComponent.OnRadioTransmitterStatusChanged += __instance.method_9;

					if (coopHandler.MyPlayer == player)
					{
						//Todo: Might have to patch method_6 as the host and other clients might need to know IsAgressorInLighthouseTraderZone
						player.ActiveHealthController.OnApplyDamageByPlayer += __instance.method_6;
					}

					___action_0?.Invoke(player.ProfileId, true);
				}

				___allPlayersInZone.Add(player);

				// Skip original method
				return false;
			}
		}

		internal class LighthouseTraderZone_RemovePlayer_Patch : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				return typeof(LighthouseTraderZone).GetMethod(nameof(LighthouseTraderZone.RemovePlayer));
			}

			[PatchPrefix]
			public static bool Prefix(Player player, LighthouseTraderZone __instance, ref List<Player> ___allPlayersInZone,
				ref List<Player> ___allowedPlayers, ref List<Player> ___unallowedPlayers, ref Action<string, bool> ___action_0)
			{
				if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
				{
					return false;
				}

				player.OnPlayerDead -= __instance.method_7;
				player.RecodableItemsHandler.TryToGetRecodableComponent(out RadioTransmitterRecodableComponent radioTransmitterRecodableComponent);

				if (___allowedPlayers.Contains(player))
				{
					___allowedPlayers.Remove(player);

					if (radioTransmitterRecodableComponent != null)
					{
						radioTransmitterRecodableComponent.OnRadioTransmitterStatusChanged -= __instance.method_9;
					}

					if (coopHandler.MyPlayer == player)
					{
						player.ActiveHealthController.OnApplyDamageByPlayer += __instance.method_6;
					}
				}
				else
				{
					___unallowedPlayers.Remove(player);
				}

				___allPlayersInZone.Remove(player);

				return false;
			}
		}

		internal class LighthouseTraderZone_Awake_Patch : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				return typeof(LighthouseTraderZone).GetMethod(nameof(LighthouseTraderZone.Awake));
			}

			[PatchPrefix]
			public static bool Prefix(LighthouseTraderZone __instance)
			{
				if (FikaBackendUtils.IsClient)
				{
					GameObject.Destroy(__instance);
					return false;
				}
				return true;
			}
		}
	}
}
