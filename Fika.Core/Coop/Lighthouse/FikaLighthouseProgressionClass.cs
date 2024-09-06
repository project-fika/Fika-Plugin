using Comfort.Common;
using EFT;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.GameWorld;
using LiteNetLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Coop.Lighthouse
{

	/// <summary>
	/// Based on <see href="https://dev.sp-tarkov.com/SPT/Modules/src/branch/3.9.x-DEV/project/SPT.SinglePlayer/Models/Progression/LighthouseProgressionClass.cs"/>
	/// </summary>
	public class FikaLighthouseProgressionClass : MonoBehaviour
	{
		public RecodableItemClass Transmitter { get; private set; }
		public List<CoopPlayer> PlayersWithDSP { get; private set; } = new();

		private CoopHandler CoopHandler;
		private GameWorld GameWorld;
		private FikaServer Server;

		private List<string> ZryachiyAndFollowersIds = new List<string>();
		private bool Aggressor = false;
		private float _timer;
		private readonly string _transmitterId = "62e910aaf957f2915e0a5e36";
		private readonly string _lightKeeperTid = "638f541a29ffd1183d187f57";

		public void Start()
		{
			CoopHandler = CoopHandler.GetCoopHandler();
			GameWorld = Singleton<GameWorld>.Instance;

			if (CoopHandler == null || CoopHandler.MyPlayer == null)
			{
				Destroy(this);

				return;
			}

			// Get transmitter from players inventory
			Transmitter = GetTransmitterFromInventory();

			if (PlayerHasActiveTransmitterInInventory())
			{
				// Give access to Lightkeepers door
				GameWorld.BufferZoneController.SetPlayerAccessStatus(CoopHandler.MyPlayer.ProfileId, true);
			}

			if (FikaBackendUtils.IsServer)
			{
				Server = Singleton<FikaServer>.Instance;

				foreach (CoopPlayer player in CoopHandler.HumanPlayers)
				{
					RecodableItemClass DSP = (RecodableItemClass)player.Profile.Inventory.AllRealPlayerItems.FirstOrDefault(x => x.TemplateId == _transmitterId);

					if (DSP == null)
					{
						FikaPlugin.Instance.FikaLogger.LogDebug($"No valid DSP found on {player.Profile.Nickname}");
						continue;
					}

					if (DSP?.RecodableComponent?.Status == RadioTransmitterStatus.Green)
					{
						FikaPlugin.Instance.FikaLogger.LogDebug($"DSP found on {player.Profile.Nickname}");

						PlayersWithDSP.Add(player);
					}
				}
			}
		}

		public void Update()
		{
			if (FikaBackendUtils.IsServer)
			{
				IncrementLastUpdateTimer();

				// Exit early if last update() run time was < 10 secs ago
				if (_timer < 10f)
				{
					return;
				}

				if (GameWorld == null)
				{
					return;
				}

				// Find Zryachiy and prep him
				if (ZryachiyAndFollowersIds.Count == 0)
				{
					SetupZryachiyAndFollowerHostility();
				}
			}
		}

		/// <summary>
		/// Gets transmitter from players inventory
		/// </summary>
		private RecodableItemClass GetTransmitterFromInventory()
		{
			return (RecodableItemClass)CoopHandler.MyPlayer.Profile.Inventory.AllRealPlayerItems.FirstOrDefault(x => x.TemplateId == _transmitterId);
		}

		/// <summary>
		/// Checks for transmitter status and exists in players inventory
		/// </summary>
		private bool PlayerHasActiveTransmitterInInventory()
		{
			return Transmitter != null &&
				   Transmitter?.RecodableComponent?.Status == RadioTransmitterStatus.Green;
		}

		/// <summary>
		/// Update _time to diff from last run of update()
		/// </summary>
		private void IncrementLastUpdateTimer()
		{
			_timer += Time.deltaTime;
		}

		/// <summary>
		/// Put Zryachiy and followers into a list and sub to their death event
		/// Make player agressor if player kills them.
		/// </summary>
		private void SetupZryachiyAndFollowerHostility()
		{
			// Only process non-players (ai)
			foreach (CoopPlayer player in CoopHandler.Players.Values)
			{
				if (ZryachiyAndFollowersIds.Contains(player.ProfileId))
				{
					continue;
				}

				if (player.IsYourPlayer || player.AIData.BotOwner == null)
				{
					continue;
				}

				// Edge case of bossZryachiy not being hostile to player
				if (player.AIData.BotOwner.IsRole(WildSpawnType.bossZryachiy) || player.AIData.BotOwner.IsRole(WildSpawnType.followerZryachiy))
				{
					AddZryachiyOrFollower(player);
				}
			}
		}

		private void AddZryachiyOrFollower(CoopPlayer bot)
		{
			// Remove valid players from being targetted by the boss & his followers
			foreach (CoopPlayer player in PlayersWithDSP)
			{
				bot.AIData.BotOwner.BotsGroup.RemoveEnemy(player);
			}

			bot.OnPlayerDead += OnZryachiyOrFollowerDeath;

			ZryachiyAndFollowersIds.Add(bot.ProfileId);
		}

		private void OnZryachiyOrFollowerDeath(Player player, IPlayer lastAggressor, DamageInfo damageInfo, EBodyPart part)
		{
			player.OnPlayerDead -= OnZryachiyOrFollowerDeath;

			LightkeeperGuardDeathPacket packet = new LightkeeperGuardDeathPacket()
			{
				ProfileId = player.KillerId
			};

			if (player.AIData.BotOwner.IsRole(WildSpawnType.bossZryachiy))
			{
				packet.WildType = WildSpawnType.bossZryachiy;
			}
			else
			{
				packet.WildType = WildSpawnType.followerZryachiy;
			}

			Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);

			// Process for server immediately.
			this.HandlePacket(packet);
		}

		public void HandlePacket(LightkeeperGuardDeathPacket packet)
		{
			// Do not run this if player does not have a DSP
			if (Transmitter == null || Aggressor)
			{
				return;
			}

			// bossZryachiy dead, close off access to LK for all players
			if (packet.WildType == WildSpawnType.bossZryachiy)
			{
				GameWorld.BufferZoneController.SetInnerZoneAvailabilityStatus(false, EFT.BufferZone.EBufferZoneData.DisableByZryachiyDead);
			}

			// Deny access to LK for player and decode DSP
			if (CoopHandler.MyPlayer.ProfileId == packet.ProfileId)
			{
				GameWorld.BufferZoneController.SetPlayerAccessStatus(packet.ProfileId, false);
				Transmitter?.RecodableComponent?.SetStatus(RadioTransmitterStatus.Red);
				Transmitter?.RecodableComponent?.SetEncoded(false);
				CoopHandler.MyPlayer.Profile.TradersInfo[_lightKeeperTid].SetStanding(-0.01);

				Aggressor = true;
			}
		}
	}
}
