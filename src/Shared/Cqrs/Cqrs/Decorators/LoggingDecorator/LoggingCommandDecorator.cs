using Ardalis.Result;
using Cqrs.Events.DomainEvents;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Messaging;
using SharedKernel.Events;

namespace Cqrs.Decorators.LoggingDecorator;

public class LoggingCommandDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    ILoggingBehaviour loggingBehaviour
    ) : CommandHandlerDecorator<TCommand, TResponse>(innerHandler)
    where TCommand : ICommand<TResponse>
{
    public async override Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        string commandName = $"Handler of {command.GetType().Name}";
        return await loggingBehaviour.ExecuteAsync(
              () => HandleInner(command, cancellationToken)
              , commandName);
    }
}

public class LoggingCommandDecorator<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    ILoggingBehaviour loggingBehaviour
    ) : CommandHandlerDecorator<TCommand>(innerHandler)
    where TCommand : ICommand
{
    public async override Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        string commandName = $"Handler of {command.GetType().Name}";

        return await loggingBehaviour.ExecuteAsync(
              () => HandleInner(command, cancellationToken)
              , commandName);
    }
}

public class DomainEventHandlerLoggingDecorator<TEvent>(
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

public class IntegrationEventHandlerLoggingDecorator<TEvent>(
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
