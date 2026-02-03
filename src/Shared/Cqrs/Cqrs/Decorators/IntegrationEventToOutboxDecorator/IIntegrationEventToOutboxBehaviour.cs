
using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.Decorators.IntegrationEventToOutboxDecorator;

public interface IIntegrationEventToOutboxBehaviour : IBehaviour
{
    Task<TResult> ExecuteAsync<TInput, TResult>(HandlerBase<TInput, TResult> handler, TInput input, CancellationToken cancellationToken) where TResult : IResult;
}
