
using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.Decorators.AtomicTransactionDecorator;


public class AtomicTransactionCommandDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    IAtomicTransactionBehaviour behaviour)
    : CommandHandlerDecorator<TCommand, TResponse>(innerHandler)
    where TCommand : ICommand<TResponse>
{
    public override Task<Result<TResponse>> HandleAsync(TCommand input, CancellationToken cancellationToken)
    {
        return behaviour.ExecuteAsync(() => HandleInner(input, cancellationToken), cancellationToken);
    }
}

public class AtomicTransactionCommandDecorator<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    IAtomicTransactionBehaviour behaviour)
    : CommandHandlerDecorator<TCommand>(innerHandler)
    where TCommand : ICommand
{
    public override Task<Result> HandleAsync(TCommand input, CancellationToken cancellationToken)
    {
        return behaviour.ExecuteAsync(() => HandleInner(input, cancellationToken), cancellationToken);
    }
}
