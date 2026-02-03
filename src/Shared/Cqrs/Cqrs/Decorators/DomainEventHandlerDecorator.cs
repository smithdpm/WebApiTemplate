

using Ardalis.Result;
using Cqrs.Events.DomainEvents;
using SharedKernel.Events;

namespace Cqrs.Decorators;

public abstract class DomainEventHandlerDecorator<TEvent>(IDomainEventHandler<TEvent> inner)
    : HandlerDecorator<TEvent, Result>(inner), IDomainEventHandler<TEvent>
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

