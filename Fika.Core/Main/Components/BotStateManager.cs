using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Main.Components
{
    public class BotStateManager : MonoBehaviour
    {
        private List<CoopBot> _bots;
        private HostGameController _controller;
        private BotsController _botsController;

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

        public static BotStateManager Create(AbstractGame game, FikaServer server, HostGameController hostGameController)
        {
            BotStateManager component = game.gameObject.AddComponent<BotStateManager>();
            component._controller = hostGameController;
            component._updateCount = 0;
            component._updatesPerTick = 1f / server.SendRate;
            component._bots = [];
            return component;
        }

        protected void Update()
        {
            _controller.Update?.Invoke();
            _botsController?.method_0();

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

        public void AssignBotsController(BotsController botsController)
        {
            _botsController = botsController;
        }

        public void UnassignBotsController()
        {
            _botsController = null;
        }
    }
}
