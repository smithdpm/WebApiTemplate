using Ardalis.Result;
using Cqrs.Messaging;

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
