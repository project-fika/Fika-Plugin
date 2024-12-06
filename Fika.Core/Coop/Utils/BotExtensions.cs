using BepInEx.Logging;
using EFT;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.Utils
{
	public static class BotExtensions
	{
		private static readonly ManualLogSource logger = new("BotExtensions");

		/// <summary>
		/// Returns all human players
		/// </summary>
		/// <param name="coopHandler"><see cref="CoopHandler"/> used to fetch players</param>
		/// <returns></returns>
		public static List<CoopPlayer> GetPlayers(CoopHandler coopHandler)
		{
			List<CoopPlayer> humanPlayers = [];

			// Grab all players
			foreach (CoopPlayer player in coopHandler.Players.Values)
			{
				if ((player.IsYourPlayer || player is ObservedCoopPlayer) && player.HealthController.IsAlive)
				{
					humanPlayers.Add(player);
				}
			}
			return humanPlayers;
		}

		/// <summary>
		/// Calculates the distance from all players
		/// </summary>
		/// <param name="position">The <see cref="Vector3"/> position</param>
		/// <param name="humanPlayers"><see cref="List{T}"/> of all human <see cref="CoopPlayer"/>s</param>
		/// <returns></returns>
		public static float GetDistanceFromPlayers(Vector3 position, List<CoopPlayer> humanPlayers)
		{
			float distance = float.PositiveInfinity;

			foreach (Player player in humanPlayers)
			{
				float tempDistance = Vector3.SqrMagnitude(position - player.Position);

				if (tempDistance < distance) // Get the closest distance to any player. so we dont despawn bots in a players face.
				{
					distance = tempDistance;
				}
			}
			return distance;
		}

		/// <summary>
		/// Grabs the bot furthest away from all players and returns its distance
		/// </summary>
		/// <param name="humanPlayers">List of all human <see cref="CoopPlayer"/>s</param>
		/// <param name="furthestDistance">The furthest <see cref="float"/> distance</param>
		/// <returns></returns>
		public static string GetFurthestBot(List<CoopPlayer> humanPlayers, Dictionary<string, Player> bots, out float furthestDistance)
		{
			string furthestBot = string.Empty;
			furthestDistance = 0f;

			foreach (KeyValuePair<string, Player> botKeyValuePair in bots)
			{
				if (IsInvalidBotForDespawning(botKeyValuePair))
				{
					continue;
				}

				float tempDistance = GetDistanceFromPlayers(botKeyValuePair.Value.Position, humanPlayers);

				if (tempDistance > furthestDistance) // We still want the furthest bot.
				{
					furthestDistance = tempDistance;
					furthestBot = botKeyValuePair.Key;
				}
			}

			return furthestBot;
		}

		/// <summary>
		/// Checks whether this bot is valid for despawning
		/// </summary>
		/// <param name="kvp"><see cref="KeyValuePair{TKey, TValue}"/> of <see cref="string"/> profileId and <see cref="Player"/> player</param>
		/// <returns></returns>
		public static bool IsInvalidBotForDespawning(KeyValuePair<string, Player> kvp)
		{
			if (kvp.Value == null || kvp.Value == null || kvp.Value.Position == null)
			{
#if DEBUG
				logger.LogWarning("Bot is null, skipping");
#endif
				return true;
			}

			CoopBot coopBot = (CoopBot)kvp.Value;

			if (coopBot != null)
			{
#if DEBUG
				logger.LogWarning("Bot is not started, skipping");
#endif
				return true;
			}

			WildSpawnType role = kvp.Value.Profile.Info.Settings.Role;

			if (role is not WildSpawnType.pmcUSEC and not WildSpawnType.pmcBEAR and not WildSpawnType.assault)
			{
				// We skip all the bots that are not pmcUSEC, pmcBEAR or assault. That means we never remove bosses, bossfollowers, and raiders
				return true;
			}

			return false;
		}
	}
}
