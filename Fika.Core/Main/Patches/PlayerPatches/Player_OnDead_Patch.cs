using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EFT;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using System;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Patches.PlayerPatches;

/// <summary>
/// This patch stops BSGs dogtag handling as it is poorly executed
/// </summary>
public class Player_OnDead_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LocalPlayer)
            .GetMethod(nameof(LocalPlayer.OnDead));
    }

    private static readonly Action<Player, EDamageType> _playerOnDead =
        typeof(Player).GetMethod(nameof(Player.OnDead)).CreateBaseCall<Action<Player, EDamageType>>();

    [PatchPrefix]
    public static bool Prefix(LocalPlayer __instance, EDamageType damageType)
    {
        if (__instance is not FikaPlayer)
        {
            return true;
        }
        
        if (__instance.IsAI && __instance.botPlayerCulling != null)
        {
            __instance.botPlayerCulling.SetMode(OfflinePlayerCulling.EMode.Disabled);
            __instance.botPlayerCulling._cullingObject?.Dispose();
        }

        _playerOnDead(__instance, damageType);
        return false;
    }
}
