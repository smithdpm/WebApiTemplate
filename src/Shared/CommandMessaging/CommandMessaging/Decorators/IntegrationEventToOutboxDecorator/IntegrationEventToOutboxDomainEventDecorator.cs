
using Ardalis.Result;
using Cqrs.Events.DomainEvents;
using SharedKernel.Events;

namespace Cqrs.Decorators.IntegrationEventToOutboxDecorator;

public class IntegrationEventToOutboxDomainEventDecorator<TEvent>(
    DomainEventHandler<TEvent> innerHandler,
    IIntegrationEventToOutboxBehaviour integrationEventBehaviour) : DomainEventHandlerDecorator<TEvent>(innerHandler)
    where TEvent : IDomainEvent
{
    public async override Task<Result> HandleAsync(TEvent input, CancellationToken cancellationToken)
    {
        return await integrationEventBehaviour.ExecuteAsync(innerHandler, input, cancellationToken);
    }
}