// © 2024 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.Custom
{
	public class FikaDynamicAI : MonoBehaviour
	{
		private readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("DynamicAI");
		private CoopHandler coopHandler;
		private int frameCounter;
		private int resetCounter;
		private readonly List<CoopPlayer> humanPlayers = [];
		private readonly List<CoopBot> bots = [];
		private readonly HashSet<CoopBot> disabledBots = [];
		private BotSpawner spawner;

		protected void Awake()
		{
			if (FikaPlugin.Instance.ModHandler.QuestingBotsLoaded)
			{
				logger.LogWarning("QuestingBots detected, destroying DynamicAI component. Use QuestingBots AI limiter instead!");
				Destroy(this);
			}

			if (!CoopHandler.TryGetCoopHandler(out coopHandler))
			{
				logger.LogError("Could not find CoopHandler! Destroying self");
				Destroy(this);
				return;
			}

			resetCounter = FikaPlugin.DynamicAIRate.Value switch
			{
				FikaPlugin.EDynamicAIRates.Low => 600,
				FikaPlugin.EDynamicAIRates.Medium => 300,
				FikaPlugin.EDynamicAIRates.High => 120,
				_ => 300,
			};

			spawner = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
			if (spawner == null)
			{
				logger.LogError("Could not find BotSpawner! Destroying self");
				Destroy(this);
				return;
			}

			spawner.OnBotCreated += Spawner_OnBotCreated;
			spawner.OnBotRemoved += Spawner_OnBotRemoved;
		}

		private void Spawner_OnBotRemoved(BotOwner botOwner)
		{
			CoopBot bot = (CoopBot)botOwner.GetPlayer;
			if (!bots.Remove(bot) && !FikaPlugin.DynamicAI.Value)
			{
				logger.LogWarning($"Could not remove {botOwner.gameObject.name} from bots list.");
			}

			if (disabledBots.Contains(bot))
			{
				disabledBots.Remove(bot);
			}
		}

		private void Spawner_OnBotCreated(BotOwner botOwner)
		{
			if (botOwner.IsYourPlayer || !botOwner.IsAI)
			{
				return;
			}

			if (botOwner.IsRole(WildSpawnType.exUsec))
			{
				return;
			}

			if (botOwner.IsRole(WildSpawnType.shooterBTR))
			{
				return;
			}

			if (FikaPlugin.DynamicAIIgnoreSnipers.Value)
			{
				if (botOwner.IsRole(WildSpawnType.marksman))
				{
					return;
				}
			}

			bots.Add((CoopBot)botOwner.GetPlayer);
		}

		protected void Update()
		{
			if (!FikaPlugin.DynamicAI.Value)
			{
				return;
			}

			frameCounter++;

			if (frameCounter % resetCounter == 0)
			{
				frameCounter = 0;
				foreach (CoopBot bot in bots)
				{
					CheckForPlayers(bot);
				}
			}
		}

		public void AddHumans()
		{
			foreach (CoopPlayer player in coopHandler.HumanPlayers)
			{
				// Do not count the dedicated profile as an active player
				if (player.Profile.IsDedicatedProfile())
				{
					continue;
				}
				humanPlayers.Add(player);
			}
		}

		private void DeactivateBot(CoopBot bot)
		{
			if (!bot.HealthController.IsAlive)
			{
				return;
			}

#if DEBUG
			logger.LogWarning($"Disabling {bot.gameObject.name}");
#endif
			bot.AIData.BotOwner.DecisionQueue.Clear();
			bot.AIData.BotOwner.Memory.GoalEnemy = null;
			bot.AIData.BotOwner.PatrollingData.Pause();
			bot.AIData.BotOwner.ShootData.EndShoot();
			bot.AIData.BotOwner.ShootData.CanShootByState = false;
			bot.ActiveHealthController.PauseAllEffects();
			bot.AIData.BotOwner.StandBy.StandByType = BotStandByType.paused;
			bot.AIData.BotOwner.StandBy.CanDoStandBy = false;
			bot.gameObject.SetActive(false);

			if (!disabledBots.Add(bot))
			{
				logger.LogError($"{bot.gameObject.name} was already in the disabled bots list when adding!");
			}

			CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
			if (coopGame == null)
			{
				return;
			}

			foreach (CoopBot otherBot in coopGame.Bots.Values)
			{
				if (otherBot == bot)
				{
					continue;
				}

				if (!otherBot.gameObject.activeSelf)
				{
					continue;
				}

				if (otherBot.AIData.BotOwner?.Memory.GoalEnemy?.ProfileId == bot.ProfileId)
				{
					otherBot.AIData.BotOwner.Memory.GoalEnemy = null;
				}
			}
		}

		private void ActivateBot(CoopBot bot)
		{
#if DEBUG
			logger.LogWarning($"Enabling {bot.gameObject.name}");
#endif
			bot.gameObject.SetActive(true);
			bot.AIData.BotOwner.PatrollingData.Unpause();
			bot.ActiveHealthController.UnpauseAllEffects();
			bot.AIData.BotOwner.StandBy.Activate();
			bot.AIData.BotOwner.StandBy.CanDoStandBy = true;
			bot.AIData.BotOwner.ShootData.CanShootByState = true;
			bot.AIData.BotOwner.ShootData.BlockFor(1f);
			disabledBots.Remove(bot);
		}

		private void CheckForPlayers(CoopBot bot)
		{
			// Do not run on bots that have no initialized yet
			if (bot.AIData.BotOwner.BotState != EBotState.Active)
			{
				return;
			}

			int notInRange = 0;
			float range = FikaPlugin.DynamicAIRange.Value;

			foreach (CoopPlayer humanPlayer in humanPlayers)
			{
				if (humanPlayer == null)
				{
					notInRange++;
					continue;
				}

				if (!humanPlayer.HealthController.IsAlive)
				{
					notInRange++;
					continue;
				}

				float distance = Vector3.SqrMagnitude(bot.Position - humanPlayer.Position);

				if (distance > range * range)
				{
					notInRange++;
				}
			}

			if (notInRange >= humanPlayers.Count && bot.gameObject.activeSelf)
			{
				DeactivateBot(bot);
			}
			else if (notInRange < humanPlayers.Count && !bot.gameObject.activeSelf)
			{
				ActivateBot(bot);
			}
		}

		public void EnabledChange(bool value)
		{
			if (!value)
			{
				CoopBot[] disabledBotsArray = [.. disabledBots];
				for (int i = 0; i < disabledBotsArray.Length; i++)
				{
					ActivateBot(disabledBotsArray[i]);
				}

				disabledBots.Clear();
			}
		}

		internal void RateChanged(FikaPlugin.EDynamicAIRates value)
		{
			resetCounter = value switch
			{
				FikaPlugin.EDynamicAIRates.Low => 600,
				FikaPlugin.EDynamicAIRates.Medium => 300,
				FikaPlugin.EDynamicAIRates.High => 120,
				_ => 300,
			};
		}
	}
}
