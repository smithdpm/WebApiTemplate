
using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.Decorators.IntegrationEventToOutboxDecorator;

public class IntegrationEventToOutboxCommandDecorator<TCommand, TResponse>(
    CommandHandler<TCommand, TResponse> innerHandler,
    IIntegrationEventToOutboxBehaviour integrationEventBehaviour) : CommandHandlerDecorator<TCommand, TResponse>(innerHandler)
    where TCommand : ICommand<TResponse>
{
    public async override Task<Result<TResponse>> HandleAsync(TCommand input, CancellationToken cancellationToken)
    {
        return await integrationEventBehaviour.ExecuteAsync(innerHandler, input, cancellationToken);
    }
}

public class IntegrationEventToOutboxCommandDecorator<TCommand>(
    CommandHandler<TCommand> innerHandler,
    IIntegrationEventToOutboxBehaviour integrationEventBehaviour) : CommandHandlerDecorator<TCommand>(innerHandler)
    where TCommand : ICommand
{
    public async override Task<Result> HandleAsync(TCommand input, CancellationToken cancellationToken)
    {
        return await integrationEventBehaviour.ExecuteAsync(innerHandler, input, cancellationToken);
    }
}

