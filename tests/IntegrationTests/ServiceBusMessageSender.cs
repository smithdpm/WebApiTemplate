using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Cqrs.Events.IntegrationEvents;


namespace IntegrationTests;
public class ServiceBusMessageSender
{
    private readonly ServiceBusClient _client;
    public ServiceBusMessageSender(string connectionString)
    {
        _client = new ServiceBusClient(connectionString);
    }
    public async Task SendMessageAsync(List<IntegrationEventBase> integrationEvents, string queueOrTopicName, CancellationToken cancellationToken = default)
    {
        var sender = _client.CreateSender(queueOrTopicName);

        Queue<ServiceBusMessage> messages = new();

        foreach (var integrationEvent in integrationEvents)
        {
            var cloudEvent = ToCloudEvent(integrationEvent);
            var message = new ServiceBusMessage(new BinaryData(cloudEvent))
            {
                ContentType = "application/cloudevents+json"

            };
            messages.Enqueue(message);
        }

        await SendBatch(sender, messages, cancellationToken);
    }

    CloudEvent ToCloudEvent(IntegrationEventBase integrationEvent)
    {
        var cloudEvent = new CloudEvent(
            source: "ServiceBusMessageSender",
            type: integrationEvent.GetType().Name,
            jsonSerializableData: integrationEvent,
            dataSerializationType: integrationEvent.GetType()
            );

        cloudEvent.Id = integrationEvent.Id.ToString();
        cloudEvent.Time = integrationEvent.Timestamp;
        return cloudEvent;
    }
    private async Task SendBatch(ServiceBusSender sender, Queue<ServiceBusMessage> messages, CancellationToken cancellationToken)
    {
        int messageCount = messages.Count;

        while (messages.Count > 0)
        {
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

            AddFirstMessageOrThrowErrorIfTooLarge(messageBatch, messages, messageCount - messageCount);

            while (messages.Count > 0 && messageBatch.TryAddMessage(messages.Peek()))
            {
                messages.Dequeue();
            }

            await sender.SendMessagesAsync(messageBatch);
        }
    }

    private void AddFirstMessageOrThrowErrorIfTooLarge(ServiceBusMessageBatch messageBatch, Queue<ServiceBusMessage> messages, int messageNo)
    {
        if (messageBatch.TryAddMessage(messages.Peek()))
        {
            messages.Dequeue();
        }
        else
        {
            throw new Exception($"Message {messageNo} is too large and cannot be sent.");
        }
    }
}
