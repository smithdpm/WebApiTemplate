using Ardalis.Result;

namespace Cqrs.Messaging;

public abstract class QueryHandler<TQuery, TResponse> : HandlerBase<TQuery, Result<TResponse>>, IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;