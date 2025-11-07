using Ardalis.Result;
using Azure.Messaging;
using SharedKernel.Events.IntegrationEvents;


namespace Infrastructure.Events.ServiceBus;
public static class IntegrationEventExtensions
{
    public static CloudEvent ToCloudEvent(this IntegrationEventBase integrationEvent)
    {
        var cloudEvent = new CloudEvent(
            source: AppDomain.CurrentDomain.FriendlyName,
            type: nameof(integrationEvent),
            jsonSerializableData: integrationEvent,
            dataSerializationType: integrationEvent.GetType()
            );
        cloudEvent.Id = integrationEvent.Id.ToString();
        cloudEvent.Time = integrationEvent.Timestamp;
        return cloudEvent;
    }
}
