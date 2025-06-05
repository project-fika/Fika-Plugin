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
        private List<CoopBot> _bots;
        private Action _onUpdate;

        private float _updateCount;
        private float _updatesPerTick;

        public bool AddBot(CoopBot bot)
        {
            if (_bots.Contains(bot))
            {
                return false;
            }

            _bots.Add(bot);
            return true;
        }

        public bool RemoveBot(CoopBot bot)
        {
            return _bots.Remove(bot);
        }

        public static BotStateManager Create(IFikaGame game, FikaServer server, Action onUpdate)
        {
            if (game is not MonoBehaviour mono)
            {
                throw new NullReferenceException("Mono missing");
            }

            BotStateManager component = mono.gameObject.AddComponent<BotStateManager>();
            component._onUpdate = onUpdate;
            component._updateCount = 0;
            component._updatesPerTick = 1f / server.SendRate;
            component._bots = [];
            return component;
        }

        protected void Update()
        {
            _onUpdate?.Invoke();

            _updateCount += Time.unscaledDeltaTime;
            if (_updateCount >= _updatesPerTick)
            {
                for (int i = _bots.Count - 1; i >= 0; i--)
                {
                    CoopBot bot = _bots[i];
                    if (!bot.HealthController.IsAlive)
                    {
                        _bots.Remove(bot);
                        continue;
                    }
                    bot.BotPacketSender.SendPlayerState();
                }
                _updateCount -= _updatesPerTick;
            }
        }

        protected void OnDestroy()
        {
            _bots.Clear();
        }
    }
}
