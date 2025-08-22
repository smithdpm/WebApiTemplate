using Application.Abstractions.Messaging;
using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Serilog.Context;
//using SharedKernel;

namespace Application.Abstractions.Behaviours;

internal static class LoggingDecorator
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            string commandName = command.GetType().Name;

            logger.LogInformation("Handling command: {CommandName}", commandName);

            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

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

    internal sealed class CommandHandler<TCommand>(ICommandHandler<TCommand> innerHandler,
            ILogger<TCommand> logger) : ICommandHandler<TCommand>
            where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            string commandName = command.GetType().Name;
            logger.LogInformation("Handling command: {commandname}", commandName);
            
            var result = await innerHandler.Handle(command, cancellationToken);

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
}
