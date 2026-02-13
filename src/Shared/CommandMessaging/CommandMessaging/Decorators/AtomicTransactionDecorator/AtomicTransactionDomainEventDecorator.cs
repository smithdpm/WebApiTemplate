
using Ardalis.Result;
using Cqrs.Events.DomainEvents;
using SharedKernel.Events;

namespace Cqrs.Decorators.AtomicTransactionDecorator;

public class AtomicTransactionDomainEventDecorator<TEvent>(
    IDomainEventHandler<TEvent> innerHandler,
    IAtomicTransactionBehaviour behaviour)
    : DomainEventHandlerDecorator<TEvent>(innerHandler)
    where TEvent : IDomainEvent
{
    public override Task<Result> HandleAsync(TEvent input, CancellationToken cancellationToken)
    {
        return behaviour.ExecuteAsync(() => HandleInner(input, cancellationToken), cancellationToken);
    }
}