using System.Reflection;
using EFT;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;

namespace Fika.Core.UI.Patches;

public sealed class MatchmakerOfflineRaidScreen_Close_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchmakerOfflineRaidScreen)
            .GetMethod(nameof(MatchmakerOfflineRaidScreen.Close));
    }

    [PatchPrefix]
    public static void Prefix(ref RaidSettings ____raidSettings, RaidSettings ____offlineRaidSettings)
    {
        ____raidSettings.TimeAndWeatherSettings = ____offlineRaidSettings.TimeAndWeatherSettings;
        ____raidSettings.WavesSettings = ____offlineRaidSettings.WavesSettings;
        ____raidSettings.MetabolismDisabled = ____offlineRaidSettings.MetabolismDisabled;
        ____raidSettings.PlayersSpawnPlace = ____offlineRaidSettings.PlayersSpawnPlace;
    }
}
