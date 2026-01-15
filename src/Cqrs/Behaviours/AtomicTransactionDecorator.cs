

using Ardalis.Result;
using Cqrs.Messaging;
using SharedKernel.Database;

namespace Cqrs.Behaviours;
public class AtomicTransactionDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    IUnitOfWork unitOfWork) 
    : CommandHandlerDecorator<TCommand, TResponse>(innerHandler)
    where TCommand : ICommand<TResponse>
{
    public override async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var result = await HandleInner(command, cancellationToken);
        if (result.IsSuccess && unitOfWork.HasChanges())
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return result;
    }
}

public class AtomicTransactionDecorator<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    IUnitOfWork unitOfWork)
    : CommandHandlerDecorator<TCommand>(innerHandler)
    where TCommand : ICommand
{
    public override async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var result = await HandleInner(command, cancellationToken);
        if (result.IsSuccess && unitOfWork.HasChanges())
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return result;
    }
}