using System;

namespace FlowBlast.Core.Events
{
    public interface IGameEventBus
    {
        void Subscribe<TEvent>(Action<TEvent> handler);
        void Unsubscribe<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent gameEvent);
    }
}
