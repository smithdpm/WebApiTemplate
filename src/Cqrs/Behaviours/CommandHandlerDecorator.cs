

using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Messaging;

namespace Cqrs.Behaviours;
public abstract class CommandHandlerDecorator<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly ICommandHandler<TCommand, TResponse> _innerHandler;
    protected CommandHandlerDecorator(ICommandHandler<TCommand, TResponse> innerHandler)
    {
        _innerHandler = innerHandler;
    }
    public bool HandlesIntegrationEvents => _innerHandler is IHasIntegrationEvents;
    public IReadOnlyDictionary<string, List<IntegrationEventBase>> IntegrationEventsToSend()
    {
        if (HandlesIntegrationEvents)
        {
            var hasIntegrationEventsHandler = (IHasIntegrationEvents)_innerHandler;
            return hasIntegrationEventsHandler.IntegrationEventsToSend;
        }

        return new Dictionary<string, List<IntegrationEventBase>>();
    }
    public abstract Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);

    protected async Task<Result<TResponse>> HandleInner(TCommand command, CancellationToken cancellationToken = default)
        => await _innerHandler.Handle(command, cancellationToken);

}

public abstract class CommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _innerHandler;
    protected CommandHandlerDecorator(ICommandHandler<TCommand> inner)
    {
        _innerHandler = inner;
    }
    public bool HandlesIntegrationEvents => _innerHandler is IHasIntegrationEvents;
    public IReadOnlyDictionary<string, List<IntegrationEventBase>> IntegrationEventsToSend()
    {
        if (HandlesIntegrationEvents)
        {
            var hasIntegrationEventsHandler = (IHasIntegrationEvents)_innerHandler;
            return hasIntegrationEventsHandler.IntegrationEventsToSend;
        }

        return new Dictionary<string, List<IntegrationEventBase>>();
    }
    public abstract Task<Result> Handle(TCommand command, CancellationToken cancellationToken);

    protected async Task<Result> HandleInner(TCommand command, CancellationToken cancellationToken = default)
        => await _innerHandler.Handle(command, cancellationToken);
}