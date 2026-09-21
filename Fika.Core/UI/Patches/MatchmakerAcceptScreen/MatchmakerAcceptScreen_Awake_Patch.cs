using System.Reflection;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches.MatchmakerAcceptScreen;

public class MatchmakerAcceptScreen_Awake_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchMakerAcceptScreen)
            .GetMethod(nameof(MatchMakerAcceptScreen.Awake));
    }

    [PatchPrefix]
    private static bool PatchPrefix(MatchMakerAcceptScreen __instance)
    {
        FikaBackendUtils.MatchMakerAcceptScreenInstance = __instance;
        FikaBackendUtils.PlayersRaidReadyPanel = __instance._playersRaidReadyPanel;
        FikaBackendUtils.MatchMakerGroupPreview = __instance._groupPreview;
        return true;
    }

}









