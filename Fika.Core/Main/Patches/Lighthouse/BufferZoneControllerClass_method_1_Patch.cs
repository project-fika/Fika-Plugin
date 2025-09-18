using EFT;
using EFT.Interactive;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace Fika.Core.Main.Patches.Lighthouse;

public class BufferZoneControllerClass_method_1_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BufferZoneControllerClass)
            .GetMethod(nameof(BufferZoneControllerClass.method_1));
    }

    [PatchPrefix]
    public static bool Prefix(EGameType gameType, BufferZoneControllerClass __instance, ref bool ___Bool_1, ref Action ___action_0)
    {
        AbstractGame.OnGameTypeSetted -= __instance.method_1;

        ___Bool_1 = gameType == EGameType.Offline;

        if (FikaBackendUtils.IsClient)
        {
            ___Bool_1 = false;
        }

        if (___Bool_1)
        {
            Player.OnPlayerDeadStatic += __instance.method_2;
            LighthouseTraderZone.OnPlayerAllowStatusChanged += __instance.method_4;
        }

        // Fire OnInitialized
        ___action_0?.Invoke();

        // Skip the original method
        return false;
    }
}
