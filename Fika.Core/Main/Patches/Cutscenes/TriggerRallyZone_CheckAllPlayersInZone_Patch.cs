using System.Reflection;
using EFT.GameTriggers;
using Fika.Core.Main.Components;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;
 
public class TriggerRallyZone_CheckAllPlayersInZone_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TriggerRallyZone), "CheckAllPlayersInZone");
    }

    [PatchPrefix]
    public static bool Prefix(TriggerRallyZone __instance, out bool __state)
    {
        __state = __instance._wasTriggered;
        return !FikaBackendUtils.IsClient;
    }

    [PatchPostfix]
    public static void Postfix(TriggerRallyZone __instance, int originPlayerRaidId, bool __state)
    {
        if (FikaBackendUtils.IsServer && !__state && __instance._wasTriggered)
        {
            FikaStoryTriggers.SendTrigger(__instance, originPlayerRaidId);
        }
    }
}
