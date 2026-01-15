
using Azure.Messaging;
using Cqrs.Events.IntegrationEvents;


namespace Cqrs.Events;
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
}
