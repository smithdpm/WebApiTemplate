

using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.Decorators;


public abstract class HandlerDecorator<TInput, TResult> : HandlerBase<TInput, TResult>
    where TResult : IResult
{
    private readonly IHandler<TInput, TResult> _innerHandler;
    protected HandlerDecorator(IHandler<TInput, TResult> innerHandler)
    {
        _innerHandler = innerHandler;
    }
    protected async Task<TResult> HandleInner(TInput input, CancellationToken cancellationToken = default)
        => await _innerHandler.HandleAsync(input, cancellationToken);
}

