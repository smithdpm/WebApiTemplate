
using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.Events.IntegrationEvents;

public abstract class IntegrationEventHandler<TEvent> : HandlerWithEventsBase<TEvent, Result>, IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEvent
{
    public Type EventType => typeof(TEvent);
    async Task<Result> IIntegrationEventHandler.HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (integrationEvent is TEvent typedEvent)
        {
            return await HandleAsync(typedEvent, cancellationToken);
        }
        else
        {
            throw new ArgumentException($"Invalid event type. Expected {typeof(TEvent).Name}, but received {integrationEvent.GetType().Name}.");
        }
    }
}