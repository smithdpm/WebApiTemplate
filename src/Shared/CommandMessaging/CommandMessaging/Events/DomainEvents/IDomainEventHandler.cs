using Ardalis.Result;
using Cqrs.Messaging;
using SharedKernel.Events;

namespace Cqrs.Events.DomainEvents;

public interface IDomainEventHandler<TEvent>: IDomainEventHandler, IHandler<TEvent, Result>
    where TEvent : IDomainEvent;

public interface IDomainEventHandler
{
    Type EventType { get; }
    Task<Result> HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
