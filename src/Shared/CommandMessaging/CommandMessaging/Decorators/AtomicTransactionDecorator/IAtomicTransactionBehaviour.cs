
using Ardalis.Result;

namespace Cqrs.Decorators.AtomicTransactionDecorator;

public interface IAtomicTransactionBehaviour : IBehaviour
{
    public Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default)
        where TResult : IResult;
}
