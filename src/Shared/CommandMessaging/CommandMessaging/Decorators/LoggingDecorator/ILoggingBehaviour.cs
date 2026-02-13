using Ardalis.Result;

namespace Cqrs.Decorators.LoggingDecorator;

public interface ILoggingBehaviour: IBehaviour  
{
    public Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, string operationName)
        where TResult : IResult;
}
