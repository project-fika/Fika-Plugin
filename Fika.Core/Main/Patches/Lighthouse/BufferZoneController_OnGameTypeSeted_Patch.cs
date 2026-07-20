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
    public static bool Prefix(EGameType gameType, BufferZoneController __instance, ref bool ___Bool_1, ref Action ___action_0)
    {
        AbstractGame.OnGameTypeSetted -= __instance.OnGameTypeSeted;

        ___Bool_1 = gameType == EGameType.Offline;

        if (FikaBackendUtils.IsClient)
        {
            ___Bool_1 = false;
        }

        if (___Bool_1)
        {
            Player.OnPlayerDeadStatic += __instance.OnPlayerKilled;
            LighthouseTraderZone.OnPlayerAllowStatusChanged += __instance.OnPlayerAllowStatusChanged;
        }

        // Fire OnInitialized
        ___action_0?.Invoke();

        // Skip the original method
        return false;
    }
}
