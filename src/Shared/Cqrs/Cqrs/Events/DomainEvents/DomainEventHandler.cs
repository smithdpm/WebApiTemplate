using SharedKernel.Events;

namespace Cqrs.Events.DomainEvents;

public abstract class DomainEventHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    public Type EventType => typeof(TEvent);

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent is TEvent typedEvent)
        {
            return HandleAsync(typedEvent, cancellationToken);
        }
        else
        {
            throw new ArgumentException($"Invalid event type. Expected {typeof(TEvent).Name}, but received {domainEvent.GetType().Name}.");
        }
    }
    public abstract Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}