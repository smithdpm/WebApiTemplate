using SharedKernel.Messaging;
using Ardalis.Result;
using Serilog.Context;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Behaviours;

public class LoggingDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    ILogger<LoggingDecorator<TCommand, TResponse>> logger
    ) : CommandHandlerDecorator<TCommand, TResponse>(innerHandler)
    where TCommand : ICommand<TResponse>
{
    public async override Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        string commandName = command.GetType().Name;

        logger.LogInformation("Handling command: {CommandName}", commandName);

        Result<TResponse> result = await HandleInner(command, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Command {CommandName} handled successfully", commandName);
        }
        else
        {
            using (LogContext.PushProperty("CommandName", result.Errors, true))
            {
                logger.LogError("Command {CommandName} completed with error", commandName);
            }
        }

        return result;
    }
}

public class LoggingDecorator<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    ILogger<LoggingDecorator<TCommand>> logger
    ) : CommandHandlerDecorator<TCommand>(innerHandler)
    where TCommand : ICommand
{
    public async override Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        string commandName = command.GetType().Name;

        logger.LogInformation("Handling command: {CommandName}", commandName);

        Result result = await HandleInner(command, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Command {CommandName} handled successfully", commandName);
        }
        else
        {
            using (LogContext.PushProperty("CommandName", result.Errors, true))
            {
                logger.LogError("Command {CommandName} completed with error", commandName);
            }
        }

        return result;
    }
}


//public static class LoggingDecorator
//{
//    public sealed class CommandHandler<TCommand, TResponse>(
//        ICommandHandler<TCommand, TResponse> innerHandler,
//        ILogger<CommandHandler<TCommand, TResponse>> logger) : ICommandHandler<TCommand, TResponse>
//        where TCommand : ICommand<TResponse>
//    {
//        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
//        {
//            string commandName = command.GetType().Name;

//            logger.LogInformation("Handling command: {CommandName}", commandName);

//            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

//            if (result.IsSuccess)
//            {
//                logger.LogInformation("Command {CommandName} handled successfully", commandName);
//            }
//            else
//            {
//                using (LogContext.PushProperty("CommandName", result.Errors, true))
//                {
//                    logger.LogError("Command {CommandName} completed with error", commandName);
//                }
//            }

//            return result;
//        }
//    }

//    public sealed class CommandHandler<TCommand>(ICommandHandler<TCommand> innerHandler,
//            ILogger<TCommand> logger) : ICommandHandler<TCommand>
//            where TCommand : ICommand
//    {
//        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
//        {
//            string commandName = command.GetType().Name;
//            logger.LogInformation("Handling command: {commandname}", commandName);

//            var result = await innerHandler.Handle(command, cancellationToken);

//            if (result.IsSuccess)
//            {
//                logger.LogInformation("Command {CommandName} handled successfully", commandName);
//            }
//            else
//            {
//                using (LogContext.PushProperty("CommandName", result.Errors, true))
//                {
//                    logger.LogError("Command {CommandName} completed with error", commandName);
//                }
//            }

//            return result;
//        }
//    }
//}
