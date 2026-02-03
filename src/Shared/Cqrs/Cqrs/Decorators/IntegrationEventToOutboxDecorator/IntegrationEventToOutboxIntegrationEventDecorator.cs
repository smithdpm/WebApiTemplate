
using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;

namespace Cqrs.Decorators.IntegrationEventToOutboxDecorator;

public class IntegrationEventToOutboxIntegrationEventDecorator<TEvent>(
    IntegrationEventHandler<TEvent> innerHandler,
    IIntegrationEventToOutboxBehaviour integrationEventBehaviour) : IntegrationEventHandlerDecorator<TEvent>(innerHandler)
    where TEvent : IIntegrationEvent
{
    public async override Task<Result> HandleAsync(TEvent input, CancellationToken cancellationToken)
    {
        return await integrationEventBehaviour.ExecuteAsync(innerHandler, input, cancellationToken);
    }
}
