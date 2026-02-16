
using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;

namespace Cqrs.Decorators.AtomicTransactionDecorator;

public class AtomicTransactionIntegrationEventDecorator<TEvent>(
    IIntegrationEventHandler<TEvent> innerHandler,
    IAtomicTransactionBehaviour behaviour)
    : IntegrationEventHandlerDecorator<TEvent>(innerHandler)
    where TEvent : IIntegrationEvent
{
    public override Task<Result> HandleAsync(TEvent input, CancellationToken cancellationToken)
    {
        return behaviour.ExecuteAsync(() => HandleInner(input, cancellationToken), cancellationToken);
    }
}
