using EFT.BufferZone;
using System;
using System.Reflection;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Lighthouse;

public class BufferZoneController_OnGameTypeSeted_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BufferZoneController)
            .GetMethod(nameof(BufferZoneController.OnGameTypeSeted));
    }

    [PatchPrefix]
    public static bool Prefix(EGameType gameType, BufferZoneController __instance)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        AbstractGame.OnGameTypeSetted -= __instance.OnGameTypeSeted;

        __instance._isOfflineMode = gameType == EGameType.Offline;

        if (FikaBackendUtils.IsClient)
        {
            __instance._isOfflineMode = false;
        }

        if (__instance._isOfflineMode)
        {
            Player.OnPlayerDeadStatic += __instance.OnPlayerKilled;
            LighthouseTraderZone.OnPlayerAllowStatusChanged += __instance.OnPlayerAllowStatusChanged;
        }

        // Fire OnInitialized
        BufferZoneController.OnInitializedField?.Invoke();

        // Skip the original method
        return false;
    }
}
