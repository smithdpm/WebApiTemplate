
namespace Cqrs.Events.IntegrationEvents;

public abstract class IntegrationEventHandler<TEvent> : IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEvent
{
    public Type EventType => typeof(TEvent);
    public Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (integrationEvent is TEvent typedEvent)
        {
            return HandleAsync(typedEvent, cancellationToken);
        }
        else
        {
            throw new ArgumentException($"Invalid event type. Expected {typeof(TEvent).Name}, but received {integrationEvent.GetType().Name}.");
        }
    }
    public abstract Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}