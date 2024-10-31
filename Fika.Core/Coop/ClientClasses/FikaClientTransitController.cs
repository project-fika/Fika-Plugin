using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fika.Core.Coop.ClientClasses
{
	public class FikaClientTransitController : GClass1643
	{
		public FikaClientTransitController(BackendConfigSettingsClass.GClass1529 settings, LocationSettingsClass.Location.TransitParameters[] parameters, Profile profile, LocalRaidSettings localRaidSettings)
			: base(settings, parameters)
		{
			OnPlayerEnter += OnClientPlayerEnter;
			OnPlayerExit += OnClientPlayerExit;
			string[] array = localRaidSettings.transition.visitedLocations.EmptyIfNull().Append(localRaidSettings.location).ToArray();
			summonedTransits[profile.Id] = new GClass1639(localRaidSettings.transition.transitionRaidId, localRaidSettings.transition.transitionCount, array);
			TransferItemsController.InitItemControllerServer(FikaGlobals.TransitTraderId, FikaGlobals.TransiterTraderName);
			this.localRaidSettings = localRaidSettings;
		}
		public GStruct176 InteractPacket { get; set; }

		private readonly LocalRaidSettings localRaidSettings;

		private void OnClientPlayerEnter(TransitPoint point, Player player)
		{
			if (!transitPlayers.ContainsKey(player.ProfileId))
			{
				TransferItemsController.InitPlayerStash(player);
				if (player is CoopPlayer coopPlayer)
				{
					coopPlayer.UpdateBtrTraderServiceData().HandleExceptions();
				}
			}
		}

		private void OnClientPlayerExit(TransitPoint point, Player player)
		{

		}

		public void Init()
		{
			method_5(dictionary_0.Values);
		}

		public override void Dispose()
		{
			base.Dispose();
			OnPlayerEnter -= OnClientPlayerEnter;
			OnPlayerExit -= OnClientPlayerExit;
		}

		public void HandleClientExtract(int transitId, int playerId)
		{
			if (!smethod_0(playerId, out Player myPlayer))
			{
				return;
			}

			if (!dictionary_0.TryGetValue(transitId, out TransitPoint transitPoint))
			{
				FikaPlugin.Instance.FikaLogger.LogError("FikaClientTransitController::HandleClientExtract: Could not find transit point with id: " + transitId);
				return;
			}

			string location = transitPoint.parameters.location;
			ERaidMode eraidMode = ERaidMode.Local;
			if (TarkovApplication.Exist(out TarkovApplication tarkovApplication))
			{
				eraidMode = ERaidMode.Local;
				tarkovApplication.transitionStatus = new GStruct136(location, false, localRaidSettings.playerSide, eraidMode, localRaidSettings.timeVariant);
			}
			string profileId = myPlayer.ProfileId;
			Dictionary<string, ProfileKey> profileKeys = [];
			profileKeys.Add(profileId, new()
			{
				_id = profileId,
				keyId = InteractPacket.keyId,
				isSolo = true
			});

			GClass1926 gclass = new()
			{
				hash = Guid.NewGuid().ToString(),
				playersCount = 1,
				ip = "",
				location = location,
				profiles = profileKeys,
				transitionRaidId = summonedTransits[profileId].raidId,
				raidMode = eraidMode,
				side = myPlayer.Side is EPlayerSide.Savage ? ESideType.Savage : ESideType.Pmc,
				dayTime = localRaidSettings.timeVariant
			};

			alreadyTransits.Add(profileId, gclass);
			if (Singleton<IFikaGame>.Instance is CoopGame coopGame)
			{
				coopGame.Extract((CoopPlayer)myPlayer, null, transitPoint);
			}
		}
	}
}
