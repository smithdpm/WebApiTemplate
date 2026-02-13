

using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.Decorators;

public abstract class QueryHandlerDecorator<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> inner)
    : HandlerDecorator<TQuery, Result<TResponse>>(inner), IQueryHandler<TQuery, TResponse>
where TQuery : IQuery<TResponse>;
