using EFT;
using EFT.Airdrop;
using Fika.Core.Coop.Airdrops.Models;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Fika.Core.Coop.Airdrops.Utils
{
	/// <summary>
	/// Originally developed by SPT <see href="https://dev.sp-tarkov.com/SPT/Modules/src/branch/master/project/SPT.Custom/Airdrops/Utils/AirdropUtil.cs"/>
	/// </summary>
	public static class FikaAirdropUtil
	{
		public static FikaAirdropConfigModel AirdropConfigModel { get; private set; }

		public static void GetConfigFromServer()
		{
			string json = RequestHandler.GetJson("/singleplayer/airdrop/config");
			AirdropConfigModel = JsonConvert.DeserializeObject<FikaAirdropConfigModel>(json);
		}

		public static int ChanceToSpawn(GameWorld gameWorld, FikaAirdropConfigModel config, bool isFlare)
		{
			// Flare summoned airdrops are guaranteed
			if (isFlare)
			{
				return 100;
			}

			string location = gameWorld.MainPlayer.Location;

			int result = 25;
			switch (location.ToLower())
			{
				case "bigmap":
					{
						result = config.AirdropChancePercent.Bigmap;
						break;
					}
				case "interchange":
					{
						result = config.AirdropChancePercent.Interchange;
						break;
					}
				case "rezervbase":
					{
						result = config.AirdropChancePercent.Reserve;
						break;
					}
				case "shoreline":
					{
						result = config.AirdropChancePercent.Shoreline;
						break;
					}
				case "woods":
					{
						result = config.AirdropChancePercent.Woods;
						break;
					}
				case "lighthouse":
					{
						result = config.AirdropChancePercent.Lighthouse;
						break;
					}
				case "tarkovstreets":
					{
						result = config.AirdropChancePercent.TarkovStreets;
						break;
					}
			}

			return result;
		}

		public static bool ShouldAirdropOccur(int dropChance, List<AirdropPoint> airdropPoints)
		{
			return airdropPoints.Count > 0 && Random.Range(0, 100) <= dropChance;
		}

		public static FikaAirdropParametersModel InitAirdropParams(GameWorld gameWorld, bool isFlare)
		{
			if (AirdropConfigModel == null)
			{
				return new FikaAirdropParametersModel()
				{
					Config = new FikaAirdropConfigModel(),
					AirdropAvailable = false
				};
			}

			if (AirdropConfigModel.AirdropChancePercent == null)
			{
				return new FikaAirdropParametersModel()
				{
					Config = new FikaAirdropConfigModel(),
					AirdropAvailable = false
				};
			}

			List<AirdropPoint> allAirdropPoints = LocationScene.GetAll<AirdropPoint>().ToList();
			Vector3 playerPosition = gameWorld.MainPlayer.Position;
			List<AirdropPoint> flareAirdropPoints = new();
			int dropChance = ChanceToSpawn(gameWorld, AirdropConfigModel, isFlare);
			float flareSpawnRadiusDistance = 100f;

			if (isFlare && allAirdropPoints.Count > 0)
			{
				foreach (AirdropPoint point in allAirdropPoints)
				{
					if (Vector3.Distance(playerPosition, point.transform.position) <= flareSpawnRadiusDistance)
					{
						flareAirdropPoints.Add(point);
					}
				}
			}

			if (flareAirdropPoints.Count == 0 && isFlare)
			{
				Debug.LogError($"[SPT-AIRDROPS]: Airdrop called in by flare, Unable to find an airdropPoint within 100m, defaulting to normal drop");
				flareAirdropPoints.Add(allAirdropPoints.OrderBy(_ => Guid.NewGuid()).FirstOrDefault());
			}

			return new FikaAirdropParametersModel()
			{
				Config = AirdropConfigModel,
				AirdropAvailable = ShouldAirdropOccur(dropChance, allAirdropPoints),

				DistanceTraveled = 0f,
				DistanceToTravel = 8000f,
				Timer = 0,
				PlaneSpawned = false,
				BoxSpawned = false,

				DropHeight = Random.Range(AirdropConfigModel.PlaneMinFlyHeight, AirdropConfigModel.PlaneMaxFlyHeight),
				TimeToStart = isFlare
					? 5
					: Random.Range(AirdropConfigModel.AirdropMinStartTimeSeconds, AirdropConfigModel.AirdropMaxStartTimeSeconds),

				RandomAirdropPoint = isFlare && allAirdropPoints.Count > 0
					? flareAirdropPoints.OrderBy(_ => Guid.NewGuid()).First().transform.position
					: allAirdropPoints.OrderBy(_ => Guid.NewGuid()).First().transform.position
			};
		}
	}
}
