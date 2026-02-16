

using Ardalis.Result;
using Cqrs.Operations.Commands;

namespace Cqrs.Decorators;

public abstract class CommandHandlerDecorator<TCommand>(ICommandHandler<TCommand> inner)
: HandlerDecorator<TCommand, Result>(inner), ICommandHandler<TCommand>
where TCommand : ICommand;

public abstract class CommandHandlerDecorator<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> inner)
    : HandlerDecorator<TCommand, Result<TResponse>>(inner), ICommandHandler<TCommand, TResponse>
where TCommand : ICommand<TResponse>;
