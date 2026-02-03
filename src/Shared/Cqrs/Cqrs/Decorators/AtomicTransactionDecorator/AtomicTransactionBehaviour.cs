
using Ardalis.Result;
using Cqrs.Extensions;
using SharedKernel.Database;

namespace Cqrs.Decorators.AtomicTransactionDecorator;

public class AtomicTransactionBehaviour(IUnitOfWork unitOfWork)
    : IAtomicTransactionBehaviour
{
    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default)
        where TResult : IResult
    {
        var result = await action();
        if (result.IsSuccess() && unitOfWork.HasChanges())
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return result;
    }
}
