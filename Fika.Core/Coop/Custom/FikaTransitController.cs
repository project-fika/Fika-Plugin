using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using System.Collections.Generic;

namespace Fika.Core.Coop.Custom
{
	public class FikaTransitController(BackendConfigSettingsClass.GClass1505 settings, LocationSettingsClass.Location.TransitParameters[] parameters, Profile profile, LocalRaidSettings localRaidSettings)
		: GClass1617(settings, parameters, profile, localRaidSettings)
	{
		LocalRaidSettings localRaidSettings = localRaidSettings;

		public override void Transit(TransitPoint point, int playersCount, string hash, Dictionary<string, ProfileKey> keys, Player player)
		{
			string location = point.parameters.location;
			ERaidMode eraidMode = ERaidMode.Local;
			if (TarkovApplication.Exist(out TarkovApplication tarkovApplication))
			{
				eraidMode = ERaidMode.Local;
				tarkovApplication.transitionStatus = new GStruct136(location, false, localRaidSettings.playerSide, eraidMode, localRaidSettings.timeVariant);
			}
			string profileId = player.ProfileId;
			GClass1883 gclass = new()
			{
				hash = hash,
				playersCount = playersCount,
				ip = "",
				location = location,
				profiles = keys,
				transitionRaidId = summonedTransits[profileId].raidId,
				raidMode = eraidMode,
				side = player.Side is EPlayerSide.Savage ? ESideType.Savage : ESideType.Pmc,
				dayTime = localRaidSettings.timeVariant
			};
			alreadyTransits.Add(profileId, gclass);
			if (Singleton<IFikaGame>.Instance is CoopGame coopGame)
			{
				coopGame.Extract((CoopPlayer)player, null, point);
				//coopGame.Stop(profileId, ExitStatus.Transit, point.parameters.name, 0f);
			}
		}
	}
}
