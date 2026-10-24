using System.Reflection;
using EFT.Vehicle;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.BTR;

/// <summary>
/// Hides the turret gunner on every peer.
/// </summary>
public class BTRTurretView_TryBotInitialization_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BTRTurretView), nameof(BTRTurretView.TryBotInitialization));
    }

    [PatchPostfix]
    public static void Postfix(BTRTurretView __instance, bool __result)
    {
        if (!__result || FikaBackendUtils.IsTutorial)
        {
            return;
        }

        var bot = __instance._offlineBot.Item1;
        if (bot != null && bot.botPlayerCulling != null)
        {
            bot.botPlayerCulling.Hide();
        }
    }
}
