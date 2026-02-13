using Ardalis.Result;

namespace Cqrs.Messaging;

public abstract class CommandHandler<TCommand>: HandlerBase<TCommand, Result>, ICommandHandler<TCommand>
    where TCommand : ICommand;
public abstract class CommandHandler<TCommand, TResponse> : HandlerBase<TCommand, Result<TResponse>>, ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
