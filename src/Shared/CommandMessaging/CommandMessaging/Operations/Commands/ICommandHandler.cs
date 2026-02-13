using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.Operations.Commands;

public interface ICommandHandler<in TCommand>: IHandler<TCommand, Result>
    where TCommand: ICommand;

public interface ICommandHandler<in TCommand, TResponse> : IHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;