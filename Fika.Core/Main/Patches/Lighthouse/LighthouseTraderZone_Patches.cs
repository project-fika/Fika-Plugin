using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using Fika.Core.Main.Components;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Lighthouse;

public static class LighthouseTraderZone_Patches
{
    public class LighthouseTraderZone_AddPlayer_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LighthouseTraderZone).GetMethod(nameof(LighthouseTraderZone.AddPlayer));
        }

        [PatchPrefix]
        public static bool Prefix(Player player, LighthouseTraderZone __instance, ref List<Player> ___allPlayersInZone,
            ref List<Player> ___allowedPlayers, ref List<Player> ___unallowedPlayers, ref Action<string, bool> ____onPlayerAllowStatusChanged)
        {
            if (!CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                return false;
            }

            player.OnPlayerDead += __instance.OnPlayerDieInZone;
            var flag = player.RecodableItemsHandler.TryToGetRecodableComponent(out RadioTransmitterRecodableComponent radioTransmitterRecodableComponent);

            if (player.IsAI && __instance.IsValidAiPlayer(player))
            {
                ___allowedPlayers.Add(player);
                ___allPlayersInZone.Add(player);

                if (coopHandler.MyPlayer == player)
                {
                    //Todo: Might have to patch SetAgressor as the host and other clients might need to know IsAgressorInLighthouseTraderZone
                    player.ActiveHealthController.OnApplyDamageByPlayer += __instance.SetAgressor;
                }

                return false;
            }
            if (!flag)
            {
                ___unallowedPlayers.Add(player);
                ___allPlayersInZone.Add(player);
                return false;
            }
            if (!__instance.IsValidPlayer(radioTransmitterRecodableComponent.Handler))
            {
                ___unallowedPlayers.Add(player);
                ____onPlayerAllowStatusChanged?.Invoke(player.ProfileId, false);
            }
            else
            {
                ___allowedPlayers.Add(player);
                radioTransmitterRecodableComponent.OnRadioTransmitterStatusChanged += __instance.OnPlayerChangeRadioTransmitterStatus;

                if (coopHandler.MyPlayer == player)
                {
                    //Todo: Might have to patch SetAgressor as the host and other clients might need to know IsAgressorInLighthouseTraderZone
                    player.ActiveHealthController.OnApplyDamageByPlayer += __instance.SetAgressor;
                }

                ____onPlayerAllowStatusChanged?.Invoke(player.ProfileId, true);
            }

            ___allPlayersInZone.Add(player);

            // Skip original method
            return false;
        }
    }

    public class LighthouseTraderZone_RemovePlayer_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LighthouseTraderZone).GetMethod(nameof(LighthouseTraderZone.RemovePlayer));
        }

        [PatchPrefix]
        public static bool Prefix(Player player, LighthouseTraderZone __instance, ref List<Player> ___allPlayersInZone,
            ref List<Player> ___allowedPlayers, ref List<Player> ___unallowedPlayers, ref Action<string, bool> ____onPlayerAllowStatusChanged)
        {
            if (!CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                return false;
            }

            player.OnPlayerDead -= __instance.OnPlayerDieInZone;
            player.RecodableItemsHandler.TryToGetRecodableComponent(out RadioTransmitterRecodableComponent radioTransmitterRecodableComponent);

            if (___allowedPlayers.Contains(player))
            {
                ___allowedPlayers.Remove(player);

                if (radioTransmitterRecodableComponent != null)
                {
                    radioTransmitterRecodableComponent.OnRadioTransmitterStatusChanged -= __instance.OnPlayerChangeRadioTransmitterStatus;
                }

                if (coopHandler.MyPlayer == player)
                {
                    player.ActiveHealthController.OnApplyDamageByPlayer += __instance.SetAgressor;
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

    public class LighthouseTraderZone_Awake_Patch : ModulePatch
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
                UnityEngine.Object.Destroy(__instance);
                return false;
            }
            return true;
        }
    }
}
