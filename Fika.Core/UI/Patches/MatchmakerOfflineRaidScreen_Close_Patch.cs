using EFT;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	public class MatchmakerOfflineRaidScreen_Close_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MatchmakerOfflineRaidScreen).GetMethod(nameof(MatchmakerOfflineRaidScreen.Close));
		}

		[PatchPrefix]
		public static void Prefix(ref RaidSettings ___raidSettings_0, RaidSettings ___raidSettings_1)
		{
			___raidSettings_0.TimeAndWeatherSettings = ___raidSettings_1.TimeAndWeatherSettings;
			___raidSettings_0.WavesSettings = ___raidSettings_1.WavesSettings;
			___raidSettings_0.MetabolismDisabled = ___raidSettings_1.MetabolismDisabled;
			___raidSettings_0.PlayersSpawnPlace = ___raidSettings_1.PlayersSpawnPlace;
		}
	}
}
