// © 2025 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using System.Collections.Generic;

namespace Fika.Core.Main.Components
{
    public class FikaDynamicAI : MonoBehaviour
    {
        private readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource("DynamicAI");
        private CoopHandler _coopHandler;
        private int _frameCounter;
        private int _resetCounter;
        private readonly List<FikaPlayer> _humanPlayers = [];
        private readonly List<FikaBot> _bots = [];
        private readonly HashSet<FikaBot> _disabledBots = [];
        private BotSpawner _spawner;

        protected void Awake()
        {
            if (FikaPlugin.Instance.ModHandler.QuestingBotsLoaded)
            {
                _logger.LogWarning("QuestingBots detected, destroying DynamicAI component. Use QuestingBots AI limiter instead!");
                Destroy(this);
            }

            if (!CoopHandler.TryGetCoopHandler(out _coopHandler))
            {
                _logger.LogError("Could not find CoopHandler! Destroying self");
                Destroy(this);
                return;
            }

            _resetCounter = FikaPlugin.DynamicAIRate.Value switch
            {
                FikaPlugin.EDynamicAIRates.Low => 600,
                FikaPlugin.EDynamicAIRates.Medium => 300,
                FikaPlugin.EDynamicAIRates.High => 120,
                _ => 300,
            };

            _spawner = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
            if (_spawner == null)
            {
                _logger.LogError("Could not find BotSpawner! Destroying self");
                Destroy(this);
                return;
            }

            _spawner.OnBotCreated += Spawner_OnBotCreated;
            _spawner.OnBotRemoved += Spawner_OnBotRemoved;
        }

        private void Spawner_OnBotRemoved(BotOwner botOwner)
        {
            FikaBot bot = (FikaBot)botOwner.GetPlayer;
            if (!_bots.Remove(bot) && !FikaPlugin.DynamicAI.Value)
            {
                _logger.LogWarning($"Could not remove {botOwner.gameObject.name} from bots list.");
            }

            if (_disabledBots.Contains(bot))
            {
                _disabledBots.Remove(bot);
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

            _bots.Add((FikaBot)botOwner.GetPlayer);
        }

        protected void Update()
        {
            if (!FikaPlugin.DynamicAI.Value)
            {
                return;
            }

            _frameCounter++;

            if (_frameCounter % _resetCounter == 0)
            {
                _frameCounter = 0;
                foreach (FikaBot bot in _bots)
                {
                    CheckForPlayers(bot);
                }
            }
        }

        public void AddHumans()
        {
            foreach (FikaPlayer player in _coopHandler.HumanPlayers)
            {
                _humanPlayers.Add(player);
            }
        }

        private void DeactivateBot(FikaBot bot)
        {
            if (!bot.HealthController.IsAlive)
            {
                return;
            }

#if DEBUG
            _logger.LogWarning($"Disabling {bot.gameObject.name}");
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

            if (!_disabledBots.Add(bot))
            {
                _logger.LogError($"{bot.gameObject.name} was already in the disabled bots list when adding!");
            }

            IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame == null)
            {
                return;
            }

            foreach (FikaBot otherBot in fikaGame.GameController.Bots.Values)
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

        private void ActivateBot(FikaBot bot)
        {
#if DEBUG
            _logger.LogWarning($"Enabling {bot.gameObject.name}");
#endif
            bot.gameObject.SetActive(true);
            bot.AIData.BotOwner.PatrollingData.Unpause();
            bot.ActiveHealthController.UnpauseAllEffects();
            bot.AIData.BotOwner.StandBy.Activate();
            bot.AIData.BotOwner.StandBy.CanDoStandBy = true;
            bot.AIData.BotOwner.ShootData.CanShootByState = true;
            bot.AIData.BotOwner.ShootData.BlockFor(1f);
            _disabledBots.Remove(bot);
        }

        private void CheckForPlayers(FikaBot bot)
        {
            // Do not run on bots that have no initialized yet
            if (bot.AIData.BotOwner.BotState != EBotState.Active)
            {
                return;
            }

            int notInRange = 0;
            float range = FikaPlugin.DynamicAIRange.Value;

            foreach (FikaPlayer humanPlayer in _humanPlayers)
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

            if (notInRange >= _humanPlayers.Count && bot.gameObject.activeSelf)
            {
                DeactivateBot(bot);
            }
            else if (notInRange < _humanPlayers.Count && !bot.gameObject.activeSelf)
            {
                ActivateBot(bot);
            }
        }

        public void EnabledChange(bool value)
        {
            if (!value)
            {
                FikaBot[] disabledBotsArray = [.. _disabledBots];
                for (int i = 0; i < disabledBotsArray.Length; i++)
                {
                    ActivateBot(disabledBotsArray[i]);
                }

                _disabledBots.Clear();
            }
        }

        internal void RateChanged(FikaPlugin.EDynamicAIRates value)
        {
            _resetCounter = value switch
            {
                FikaPlugin.EDynamicAIRates.Low => 600,
                FikaPlugin.EDynamicAIRates.Medium => 300,
                FikaPlugin.EDynamicAIRates.High => 120,
                _ => 300,
            };
        }
    }
}
