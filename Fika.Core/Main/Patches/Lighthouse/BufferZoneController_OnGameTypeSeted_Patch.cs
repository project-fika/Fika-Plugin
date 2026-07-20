using EFT.BufferZone;
using System;
using System.Reflection;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Lighthouse;

public class BufferZoneController_OnGameTypeSeted_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BufferZoneController)
            .GetMethod(nameof(BufferZoneController.OnGameTypeSeted));
    }

    [PatchPrefix]
    public static bool Prefix(EGameType gameType, BufferZoneController __instance, ref bool ____isOfflineMode, ref Action ____onInitialized)
    {
        AbstractGame.OnGameTypeSetted -= __instance.OnGameTypeSeted;

        ____isOfflineMode = gameType == EGameType.Offline;

        if (FikaBackendUtils.IsClient)
        {
            ____isOfflineMode = false;
        }

        if (____isOfflineMode)
        {
            Player.OnPlayerDeadStatic += __instance.OnPlayerKilled;
            LighthouseTraderZone.OnPlayerAllowStatusChanged += __instance.OnPlayerAllowStatusChanged;
        }

        // Fire OnInitialized
        ____onInitialized?.Invoke();

        // Skip the original method
        return false;
    }
}
