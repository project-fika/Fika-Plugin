using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using Fika.Core.Main.Components;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

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
        public static bool Prefix(Player player, LighthouseTraderZone __instance)
        {
            if (FikaBackendUtils.IsTutorial)
            {
                return true;
            }

            if (!CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                return false;
            }

            player.OnPlayerDead += __instance.OnPlayerDieInZone;
            var flag = player.RecodableItemsHandler.TryToGetRecodableComponent(out RadioTransmitterRecodableComponent radioTransmitterRecodableComponent);

            if (player.IsAI && __instance.IsValidAiPlayer(player))
            {
                __instance.allowedPlayers.Add(player);
                __instance.allPlayersInZone.Add(player);

                if (coopHandler.MyPlayer == player)
                {
                    //Todo: Might have to patch SetAgressor as the host and other clients might need to know IsAgressorInLighthouseTraderZone
                    player.ActiveHealthController.OnApplyDamageByPlayer += __instance.SetAgressor;
                }

                return false;
            }
            if (!flag)
            {
                __instance.unallowedPlayers.Add(player);
                __instance.allPlayersInZone.Add(player);
                return false;
            }
            if (!__instance.IsValidPlayer(radioTransmitterRecodableComponent.Handler))
            {
                __instance.unallowedPlayers.Add(player);
                LighthouseTraderZone.OnPlayerAllowStatusChangedField?.Invoke(player.RaidId, false);
            }
            else
            {
                __instance.allowedPlayers.Add(player);
                radioTransmitterRecodableComponent.OnRadioTransmitterStatusChanged += __instance.OnPlayerChangeRadioTransmitterStatus;

                if (coopHandler.MyPlayer == player)
                {
                    //Todo: Might have to patch SetAgressor as the host and other clients might need to know IsAgressorInLighthouseTraderZone
                    player.ActiveHealthController.OnApplyDamageByPlayer += __instance.SetAgressor;
                }

                LighthouseTraderZone.OnPlayerAllowStatusChangedField?.Invoke(player.RaidId, true);
            }

            __instance.allPlayersInZone.Add(player);

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
        public static bool Prefix(Player player, LighthouseTraderZone __instance)
        {
            if (FikaBackendUtils.IsTutorial)
            {
                return true;
            }

            if (!CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                return false;
            }

            player.OnPlayerDead -= __instance.OnPlayerDieInZone;
            player.RecodableItemsHandler.TryToGetRecodableComponent(out RadioTransmitterRecodableComponent radioTransmitterRecodableComponent);

            if (__instance.allowedPlayers.Contains(player))
            {
                __instance.allowedPlayers.Remove(player);

                if (radioTransmitterRecodableComponent != null)
                {
                    radioTransmitterRecodableComponent.OnRadioTransmitterStatusChanged -= __instance.OnPlayerChangeRadioTransmitterStatus;
                }

                if (coopHandler.MyPlayer == player)
                {
                    player.ActiveHealthController.OnApplyDamageByPlayer -= __instance.SetAgressor;
                }
            }
            else
            {
                __instance.unallowedPlayers.Remove(player);
            }

            __instance.allPlayersInZone.Remove(player);

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
            if (FikaBackendUtils.IsTutorial)
            {
                return true;
            }

            if (FikaBackendUtils.IsClient)
            {
                UnityEngine.Object.Destroy(__instance);
                return false;
            }
            return true;
        }
    }
}
