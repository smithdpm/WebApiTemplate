using Ardalis.Result;

namespace Cqrs.Messaging;

public interface IHandler<in TInput, TResult>
    where TResult : IResult
{
    Task<TResult> HandleAsync(TInput input, CancellationToken cancellationToken);
}
