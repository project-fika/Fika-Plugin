using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Jobs;
using Fika.Core.Networking;
using System.Collections.Generic;
using Unity.Jobs;
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

        public static BotStateManager Create(CoopGame game, FikaServer server)
        {
            BotStateManager component = game.gameObject.AddComponent<BotStateManager>();
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
