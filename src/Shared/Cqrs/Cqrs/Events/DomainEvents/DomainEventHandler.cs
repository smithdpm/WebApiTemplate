using Ardalis.Result;
using Cqrs.Messaging;
using SharedKernel.Events;

namespace Cqrs.Events.DomainEvents;

public abstract class DomainEventHandler<TEvent> : HandlerBase<TEvent, Result>, IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    public Type EventType => typeof(TEvent);

    async Task<Result> IDomainEventHandler.HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent is TEvent typedEvent)
        {
            return await HandleAsync(typedEvent, cancellationToken);
        }
        else
        {
            throw new ArgumentException($"Invalid event type. Expected {typeof(TEvent).Name}, but received {domainEvent.GetType().Name}.");
        }
    }
}