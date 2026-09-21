using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EFT;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using System;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using EFT.Ballistics;

namespace Fika.Core.Main.Patches.PlayerPatches;

/// <summary>
/// This patch stops BSGs dogtag handling as it is poorly executed
/// </summary>
public class Player_ManageAggressor_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LocalPlayer)
            .GetMethod(nameof(LocalPlayer.ManageAggressor));
    }

    private static readonly Action<Player, DamageInfo, EBodyPart, EBodyPartColliderType> _playerManageAggressor =
        typeof(Player).GetMethod(nameof(Player.ManageAggressor)).CreateBaseCall<Action<Player, DamageInfo, EBodyPart, EBodyPartColliderType>>();

    [PatchPrefix]
    public static bool Prefix(LocalPlayer __instance, DamageInfo damageInfo, EBodyPart bodyPart, EBodyPartColliderType colliderType)
    {
        if (__instance is not FikaPlayer)
        {
            return true;
        }

        _playerManageAggressor(__instance, damageInfo, bodyPart, colliderType);
        return false;
    }
}
