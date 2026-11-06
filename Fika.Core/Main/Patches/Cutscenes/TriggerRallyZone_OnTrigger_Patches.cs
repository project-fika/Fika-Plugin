using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.GameTriggers;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;

public static class TriggerRallyZone_OnTrigger_Patches
{
    public class Enter : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TriggerRallyZone), nameof(TriggerRallyZone.OnTriggerEnter));
        }

        [PatchPrefix]
        public static bool Prefix(TriggerRallyZone __instance, Collider other)
        {
            return Handle(__instance, other, true);
        }
    }

    public class Exit : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TriggerRallyZone), nameof(TriggerRallyZone.OnTriggerExit));
        }

        [PatchPrefix]
        public static bool Prefix(TriggerRallyZone __instance, Collider other)
        {
            return Handle(__instance, other, false);
        }
    }

    private static bool Handle(TriggerRallyZone zone, Collider other, bool entered)
    {
        var player = Singleton<GameWorld>.Instance?.GetPlayerByCollider(other);
        if (player == null || player.IsAI)
        {
            return true;
        }

        if (FikaBackendUtils.IsClient)
        {
            if (player.IsYourPlayer && player is FikaPlayer fikaPlayer)
            {
                FikaStoryTriggers.SendPresence(zone, fikaPlayer, entered);
            }

            return true;
        }

        return player.IsYourPlayer;
    }
}
