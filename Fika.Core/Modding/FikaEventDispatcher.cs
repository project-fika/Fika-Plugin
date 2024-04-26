using Fika.Core.Modding.Events;
using System;

namespace Fika.Core.Modding
{
    public static class FikaEventDispatcher
    {
        public delegate void FikaEventHandler(FikaEvent e);

        // I'm leaving this here but consumers should definitely subscribe to individual events
        public static event FikaEventHandler OnFikaEvent;

        internal static void DispatchEvent<TEvent>(TEvent e) where TEvent : FikaEvent
        {
            OnFikaEvent?.Invoke(e);
        }

        public static void SubscribeEvent<TEvent>(Action<TEvent> callback) where TEvent : FikaEvent
        {
            OnFikaEvent += e =>
            {
                if (e is TEvent specificEvent)
                {
                    callback?.Invoke(specificEvent);
                }
            };
        }

        public static void UnsubscribeEvent<TEvent>(Action<TEvent> callback) where TEvent : FikaEvent
        {
            OnFikaEvent -= e =>
            {
                if (e is TEvent specificEvent)
                {
                    callback?.Invoke(specificEvent);
                }
            };
        }
    }
}
