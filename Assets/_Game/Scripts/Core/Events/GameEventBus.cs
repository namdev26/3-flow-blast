using System;
using System.Collections.Generic;

namespace FlowBlast.Core.Events
{
    public sealed class GameEventBus : IGameEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                return;
            }

            Type eventType = typeof(TEvent);

            if (!handlers.TryGetValue(eventType, out List<Delegate> eventHandlers))
            {
                eventHandlers = new List<Delegate>();
                handlers[eventType] = eventHandlers;
            }

            eventHandlers.Add(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                return;
            }

            Type eventType = typeof(TEvent);

            if (!handlers.TryGetValue(eventType, out List<Delegate> eventHandlers))
            {
                return;
            }

            eventHandlers.Remove(handler);
        }

        public void Publish<TEvent>(TEvent gameEvent)
        {
            Type eventType = typeof(TEvent);

            if (!handlers.TryGetValue(eventType, out List<Delegate> eventHandlers))
            {
                return;
            }

            for (int i = eventHandlers.Count - 1; i >= 0; i--)
            {
                if (eventHandlers[i] is Action<TEvent> handler)
                {
                    handler.Invoke(gameEvent);
                }
            }
        }
    }
}
