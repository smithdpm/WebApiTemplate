

using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;

namespace Cqrs.Decorators;

public abstract class IntegrationEventHandlerDecorator<TEvent>(IIntegrationEventHandler<TEvent> inner)
    : HandlerDecorator<TEvent, Result>(inner), IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEvent
{
    public Type EventType => typeof(TEvent);

    async Task<Result> IIntegrationEventHandler.HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
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

