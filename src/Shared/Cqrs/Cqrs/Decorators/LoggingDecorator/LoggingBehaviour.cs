using Ardalis.Result;
using Cqrs.Extensions;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Cqrs.Decorators.LoggingDecorator;

public class LoggingBehaviour(ILogger<LoggingBehaviour> logger): ILoggingBehaviour
{
    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, string operationName)
        where TResult : IResult
    {
        logger.LogInformation("Handling operation: {OperationName}", operationName);

        var result = await action();

        if (result.IsSuccess())
        {
            logger.LogInformation("Operation {OperationName} handled successfully", operationName);
        }
        else
        {
            using (LogContext.PushProperty("CommandName", result.Errors, true))
            {
                logger.LogError("Operation {OperationName} completed with error", operationName);
            }
        }

        return result;
    }
}
