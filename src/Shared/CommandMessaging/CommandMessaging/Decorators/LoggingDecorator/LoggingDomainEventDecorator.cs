using Ardalis.Result;
using Cqrs.Events.DomainEvents;
using SharedKernel.Events;

namespace Cqrs.Decorators.LoggingDecorator;

public class LoggingDomainEventDecorator<TEvent>(
    IDomainEventHandler<TEvent> innerHandler,
    ILoggingBehaviour loggingBehaviour
    ) : DomainEventHandlerDecorator<TEvent>(innerHandler)
    where TEvent : IDomainEvent
{
    public override async Task<Result> HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)
    {
        string commandName = $"Handler of {domainEvent.GetType().Name}";

        return await loggingBehaviour.ExecuteAsync(
            () => HandleInner(domainEvent, cancellationToken),
            domainEvent.GetType().Name);
    }
}
