using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.Operations.Commands;

public abstract class CommandHandler<TCommand>: HandlerWithEventsBase<TCommand, Result>, ICommandHandler<TCommand>
    where TCommand : ICommand;
public abstract class CommandHandler<TCommand, TResponse> : HandlerWithEventsBase<TCommand, Result<TResponse>>, ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
