
using Azure.Messaging;
using Cqrs.Outbox;


namespace Cqrs.Events.IntegrationEvents;
public static class IntegrationEventExtensions
{
    public static CloudEvent ToCloudEvent(this IntegrationEventBase integrationEvent)
    {
        var cloudEvent = new CloudEvent(
            source: AppDomain.CurrentDomain.FriendlyName,
            type: integrationEvent.GetType().Name,
            jsonSerializableData: integrationEvent,
            dataSerializationType: integrationEvent.GetType()
            );
        cloudEvent.Id = integrationEvent.Id.ToString();
        cloudEvent.Time = integrationEvent.Timestamp;
        return cloudEvent;
    }

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
