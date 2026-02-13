using Ardalis.Result;

namespace Cqrs.Operations.Queries;

public abstract class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public abstract Task<Result<TResponse>> HandleAsync(TQuery input, CancellationToken cancellationToken);
}