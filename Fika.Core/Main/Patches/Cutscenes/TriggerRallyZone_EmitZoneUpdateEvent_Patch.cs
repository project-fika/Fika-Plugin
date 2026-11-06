using System.Reflection;
using EFT.GameTriggers;
using Fika.Core.Main.Components;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;

/// <summary>
///     Only the host counts who is in a rally zone. Its count goes to every client, where it fills the waiting banner the
///     way the game server's zone update does
/// </summary>
public class TriggerRallyZone_EmitZoneUpdateEvent_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TriggerRallyZone), "EmitZoneUpdateEvent");
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return !FikaBackendUtils.IsClient;
    }

    [PatchPostfix]
    public static void Postfix(TriggerRallyZone __instance)
    {
        if (FikaBackendUtils.IsServer)
        {
            FikaStoryTriggers.SendUpdate(__instance);
        }
    }
}
