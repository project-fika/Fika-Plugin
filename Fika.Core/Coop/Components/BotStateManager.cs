using Fika.Core.Coop.GameMode;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
    public class BotStateManager : MonoBehaviour
    {
        public delegate void UpdateAction();
        public event UpdateAction OnUpdate;

        private float updateCount;
        private float updatesPerTick;

        public static BotStateManager Create(CoopGame game, FikaServer server)
        {
            BotStateManager component = game.gameObject.AddComponent<BotStateManager>();
            component.updateCount = 0;
            component.updatesPerTick = 1f / server.SendRate;
            return component;
        }

        protected void Update()
        {
            updateCount += Time.unscaledDeltaTime;
            if (updateCount >= updatesPerTick)
            {
                OnUpdate?.Invoke();
                updateCount -= updatesPerTick;
            }
        }

        protected void OnDestroy()
        {
            OnUpdate = null;
        }
    }
}
