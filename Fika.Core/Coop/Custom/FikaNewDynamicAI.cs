using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.Custom
{
    public class FikaNewDynamicAI : MonoBehaviour
    {
        private ManualLogSource logger;
        private CoopHandler coopHandler;
        private int frameCounter;
        private int resetCounter;
        private List<CoopPlayer> humanPlayers = [];
        private List<CoopBot> bots = [];
        private BotSpawner spawner;

        protected void Awake()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource("NewDynamicAI");
            if (!CoopHandler.TryGetCoopHandler(out coopHandler))
            {
                logger.LogError("Could not find CoopHandler! Destroying self");
                Destroy(this);
                return;
            }

            resetCounter = FikaPlugin.DynamicAIRate.Value switch
            {
                FikaPlugin.DynamicAIRates.Low => 600,
                FikaPlugin.DynamicAIRates.Medium => 300,
                FikaPlugin.DynamicAIRates.High => 120,
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
            if (!bots.Remove((CoopBot)botOwner.GetPlayer))
            {
                logger.LogWarning($"Could not remove {botOwner.gameObject.name} from bots list.");
            }
        }

        private void Spawner_OnBotCreated(BotOwner botOwner)
        {
            bots.Add((CoopBot)botOwner.GetPlayer);
        }

        protected void Update()
        {
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
            foreach (CoopPlayer player in coopHandler.Players.Values)
            {
                if (player.IsYourPlayer || player is ObservedCoopPlayer)
                {
                    humanPlayers.Add(player);
                }
            }
        }

        private void DeactivateBot(CoopPlayer bot)
        {
#if DEBUG
            logger.LogWarning($"Disabling {bot.gameObject.name}");
#endif
            bot.AIData.BotOwner.BotState = EBotState.NonActive;
            bot.AIData.BotOwner.ShootData.EndShoot();
            bot.AIData.BotOwner.ShootData.SetCanShootByState(false);
            bot.AIData.BotOwner.DecisionQueue.Clear();
            bot.AIData.BotOwner.PatrollingData.Pause();
            bot.AIData.BotOwner.Memory.GoalEnemy = null;
            bot.gameObject.SetActive(false);
        }

        private void ActivateBot(CoopPlayer bot)
        {
#if DEBUG
            logger.LogWarning($"Enabling {bot.gameObject.name}");
#endif
            bot.gameObject.SetActive(true);
            bot.AIData.BotOwner.BotState = EBotState.Active;
            bot.AIData.BotOwner.ShootData.SetCanShootByState(true);
            bot.AIData.BotOwner.PatrollingData.Unpause();
        }

        private void CheckForPlayers(CoopBot bot)
        {
            if (!FikaPlugin.DynamicAI.Value)
            {
                if (!bot.gameObject.activeSelf)
                {
                    ActivateBot(bot);
                    return;
                }
            }

            if (!bot.HealthController.IsAlive)
            {
                return;
            }

            int notInRange = 0;

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
                float range = FikaPlugin.DynamicAIRange.Value;
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
    }
}
