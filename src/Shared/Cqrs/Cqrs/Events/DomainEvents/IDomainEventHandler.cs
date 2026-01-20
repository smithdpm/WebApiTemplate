using SharedKernel.Events;

namespace Cqrs.Events.DomainEvents;

public interface IDomainEventHandler<TEvent>: IDomainEventHandler 
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

public interface IDomainEventHandler
{
    Type EventType { get; }
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
