
using Ardalis.Result;
using SharedKernel.Database;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Messaging;
using Cqrs.Outbox;

namespace Cqrs.Decorators;

public class IntegrationEventDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    IRepository<OutboxMessage> repository) : CommandHandlerDecorator<TCommand, TResponse>(innerHandler)
    where TCommand : ICommand<TResponse>
{
    public override async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        Result<TResponse> result = await HandleInner(command, cancellationToken);

        if (HandlesIntegrationEvents && result.IsSuccess)
        {
            if (IntegrationEventsToSend().Count > 0)
            {
                await repository.AddRangeAsync(
                    IntegrationEventExtensions.IntegrationEventsToOutboxMessages(IntegrationEventsToSend()));
            }
        }

        return result;
    }
}

public class IntegrationEventDecorator<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    IRepository<OutboxMessage> repository) : CommandHandlerDecorator<TCommand>(innerHandler)
    where TCommand : ICommand
{
    public override async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        Result result = await HandleInner(command, cancellationToken);

        if (HandlesIntegrationEvents && result.IsSuccess)
        {
            if (IntegrationEventsToSend().Count > 0)
            {
                await repository.AddRangeAsync(
                    IntegrationEventExtensions.IntegrationEventsToOutboxMessages(IntegrationEventsToSend()));
            }
        }

        return result;
    }
}

public static class IntegrationEventExtensions
{
    public static List<OutboxMessage> IntegrationEventsToOutboxMessages(IReadOnlyDictionary<string, List<IntegrationEventBase>> integrationEventsToSend)
    {
        var outboxMessages = new List<OutboxMessage>();

        foreach (var eventDestination in integrationEventsToSend)
        {
            string destination = eventDestination.Key;

            foreach (var integrationEvent in eventDestination.Value)
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