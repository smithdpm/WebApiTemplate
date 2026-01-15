using SharedKernel.Events;

namespace Cqrs.Events.DomainEvents;

public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

