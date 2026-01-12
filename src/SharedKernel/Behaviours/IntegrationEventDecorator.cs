
using Ardalis.Result;
using SharedKernel.Database;
using SharedKernel.Events;
using SharedKernel.Events.IntegrationEvents;
using SharedKernel.Messaging;

namespace SharedKernel.Behaviours;
public static class IntegrationEventDecorator
{
    public sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IRepository<OutboxMessage> repository) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

            if (innerHandler is IHasIntegrationEvents && result.IsSuccess)
            {
                var hasIntegrationEventsHandler = (IHasIntegrationEvents)innerHandler;
                if (hasIntegrationEventsHandler.IntegrationEventsToSend.Count > 0)
                {
                    await repository.AddRangeAsync(
                        IntegrationEventsToOutboxMessages(hasIntegrationEventsHandler.IntegrationEventsToSend));
                }
            }

            return result;
        }
    }

    public sealed class CommandHandler<TCommand>(ICommandHandler<TCommand> innerHandler,
        IRepository<OutboxMessage> repository) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            Result result = await innerHandler.Handle(command, cancellationToken);

            if (innerHandler is IHasIntegrationEvents && result.IsSuccess)
            {
                var hasIntegrationEventsHandler = (IHasIntegrationEvents)innerHandler;
                if (hasIntegrationEventsHandler.IntegrationEventsToSend.Count > 0)
                {
                    await repository.AddRangeAsync(
                        IntegrationEventsToOutboxMessages(hasIntegrationEventsHandler.IntegrationEventsToSend));
                }
            }

            return result;
        }
    }

    private static List<OutboxMessage> IntegrationEventsToOutboxMessages(IReadOnlyDictionary<string, List<IntegrationEventBase>> integrationEventsToSend)
    {
        var outboxMessages = new List<OutboxMessage>();
        
        foreach (var eventDestination in integrationEventsToSend)
        {
            string destination = eventDestination.Key;

            foreach  (var integrationEvent in eventDestination.Value)
                outboxMessages.Add(new OutboxMessage(
                eventType: integrationEvent.GetType().Name ?? string.Empty,
                destination: destination,
                payload: System.Text.Json.JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
                occurredOnUtc: integrationEvent.Timestamp
            ));
        }

        return outboxMessages;
    }
}
