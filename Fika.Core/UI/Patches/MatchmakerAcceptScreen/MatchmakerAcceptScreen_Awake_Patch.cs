using System;
using System.Reflection;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Utils;
using Newtonsoft.Json;
using SPT.Reflection.Patching;

namespace Fika.Core.UI.Patches.MatchmakerAcceptScreen;

public class MatchmakerAcceptScreen_Awake_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchMakerAcceptScreen)
            .GetMethod(nameof(MatchMakerAcceptScreen.Awake));
    }

    [PatchPrefix]
    private static bool PatchPrefix(MatchMakerAcceptScreen __instance, PlayersRaidReadyPanel ____playersRaidReadyPanel, MatchMakerGroupPreview ____groupPreview)
    {
        FikaBackendUtils.MatchMakerAcceptScreenInstance = __instance;
        FikaBackendUtils.PlayersRaidReadyPanel = ____playersRaidReadyPanel;
        FikaBackendUtils.MatchMakerGroupPreview = ____groupPreview;
        return true;
    }

}









