

using Ardalis.Result;
using Cqrs.Operations.Queries;

namespace Cqrs.Decorators;

public abstract class QueryHandlerDecorator<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> inner)
    : HandlerDecorator<TQuery, Result<TResponse>>(inner), IQueryHandler<TQuery, TResponse>
where TQuery : IQuery<TResponse>;
