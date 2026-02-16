using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.Operations.Queries;
public interface IQueryHandler<in TQuery, TResponse>: IHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
