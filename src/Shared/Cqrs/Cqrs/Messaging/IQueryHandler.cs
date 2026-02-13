using Ardalis.Result;

namespace Cqrs.Messaging;
public interface IQueryHandler<in TQuery, TResponse>: IHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
