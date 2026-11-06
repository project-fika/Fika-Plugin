using System.Reflection;
using EFT.GameTriggers;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;

/// <summary>
///     The banner handler marks a full zone as done for display. On the host it runs right before the zone's own check, so
///     it would stop the host from ever firing the trigger. A headless host has no banner to show at all
/// </summary>
public class TriggerRallyZone_OnZoneUpdate_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TriggerRallyZone), "OnZoneUpdate");
    }

    [PatchPrefix]
    public static bool Prefix(TriggerRallyZone __instance, out bool __state)
    {
        __state = __instance._wasTriggered;
        return !FikaBackendUtils.IsHeadless;
    }

    [PatchPostfix]
    public static void Postfix(TriggerRallyZone __instance, bool __state)
    {
        if (FikaBackendUtils.IsServer)
        {
            __instance._wasTriggered = __state;
        }
    }
}
