using System.Reflection;
using EFT;
using EFT.UI.Matchmaker;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches;

public sealed class MatchmakerOfflineRaidScreen_Close_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchmakerOfflineRaidScreen)
            .GetMethod(nameof(MatchmakerOfflineRaidScreen.Close));
    }

    [PatchPrefix]
    public static void Prefix(EFT.UI.Matchmaker.MatchmakerOfflineRaidScreen __instance)
    {
        __instance._raidSettings.TimeAndWeatherSettings = __instance._offlineRaidSettings.TimeAndWeatherSettings;
        __instance._raidSettings.WavesSettings = __instance._offlineRaidSettings.WavesSettings;
        __instance._raidSettings.MetabolismDisabled = __instance._offlineRaidSettings.MetabolismDisabled;
        __instance._raidSettings.PlayersSpawnPlace = __instance._offlineRaidSettings.PlayersSpawnPlace;
    }
}
