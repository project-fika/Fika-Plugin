using Fika.Core.Modding.Events;
using System;

namespace Fika.Core.Modding;

/// <summary>
/// Provides a static event dispatcher for Fika events, allowing subscription and dispatching of events.
/// </summary>
public static class FikaEventDispatcher
{
    /// <summary>
    /// Represents a handler for Fika events.
    /// </summary>
    /// <param name="e">The event instance.</param>
    public delegate void FikaEventHandler(FikaEvent e);

    /// <summary>
    /// Occurs when any Fika event is dispatched.
    /// </summary>
    /// <remarks>Consumers are encouraged to subscribe to individual events instead.</remarks>
    public static event FikaEventHandler OnFikaEvent;

    /// <summary>
    /// Dispatches a Fika event to all registered handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event, derived from <see cref="FikaEvent"/>.</typeparam>
    /// <param name="e">The event instance to dispatch.</param>
    public static void DispatchEvent<TEvent>(TEvent e) where TEvent : FikaEvent
    {
        OnFikaEvent?.Invoke(e);
    }

    /// <summary>
    /// Subscribes a callback to a specific type of Fika event.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to subscribe to.</typeparam>
    /// <param name="callback">The callback to invoke when the event is dispatched.</param>
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

    /// <summary>
    /// Unsubscribes a callback from a specific type of Fika event.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to unsubscribe from.</typeparam>
    /// <param name="callback">The callback to remove from the event subscription.</param>
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
