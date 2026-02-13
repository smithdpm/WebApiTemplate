using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;

namespace Cqrs.Decorators.LoggingDecorator;

public class LoggingIntegrationEventDecorator<TEvent>(
    IIntegrationEventHandler<TEvent> innerHandler,
    ILoggingBehaviour loggingBehaviour
    ) : IntegrationEventHandlerDecorator<TEvent>(innerHandler)
    where TEvent : IIntegrationEvent
{
    public override async Task<Result> HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken)
    {
        string commandName = $"Handler of {integrationEvent.GetType().Name}";
        return await loggingBehaviour.ExecuteAsync(
            () => HandleInner(integrationEvent, cancellationToken),
            integrationEvent.GetType().Name);
    }
}
