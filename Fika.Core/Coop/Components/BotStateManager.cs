using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
    public class BotStateManager : MonoBehaviour
    {
        private List<CoopBot> bots;

        private float updateCount;
        private float updatesPerTick;

        public bool AddBot(CoopBot bot)
        {
            if (bots.Contains(bot))
            {
                return false;
            }

            bots.Add(bot);
            return true;
        }

        public bool RemoveBot(CoopBot bot)
        {
            return bots.Remove(bot);
        }

        public static BotStateManager Create(IFikaGame game, FikaServer server)
        {
            if (game is not MonoBehaviour mono)
            {
                throw new NullReferenceException("Mono missing");
            }

            BotStateManager component = mono.gameObject.AddComponent<BotStateManager>();
            component.updateCount = 0;
            component.updatesPerTick = 1f / server.SendRate;
            component.bots = [];
            return component;
        }

        protected void Update()
        {
            updateCount += Time.unscaledDeltaTime;
            if (updateCount >= updatesPerTick)
            {
                for (int i = bots.Count - 1; i >= 0; i--)
                {
                    CoopBot bot = bots[i];
                    if (!bot.HealthController.IsAlive)
                    {
                        bots.Remove(bot);
                        continue;
                    }
                    bot.BotPacketSender.SendPlayerState();
                }
                updateCount -= updatesPerTick;
            }
        }

        protected void OnDestroy()
        {
            bots.Clear();
        }
    }
}
