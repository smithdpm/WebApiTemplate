using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Cqrs.Events.IntegrationEvents;


namespace Cqrs.AzureServiceBus.Dispatcher;
internal class ServiceBusEventDispatcher(IAzureClientFactory<ServiceBusSender> senderFactory) : IIntegrationEventDispatcher
{
    public async Task DispatchEventsAsync(IEnumerable<IntegrationEventBase> integrationEvents, string queueOrTopic, CancellationToken cancellationToken = default)
    {
        var sender = senderFactory.CreateClient(queueOrTopic);

        Queue<ServiceBusMessage> messages = new();

        foreach (var integrationEvent in integrationEvents)
        {
            var payload = integrationEvent.ToCloudEvent();

            var message = new ServiceBusMessage(new BinaryData(payload))
            {
                ContentType = "application/cloudevents+json"

            };
            messages.Enqueue(message);
        }

        await SendBatch(sender, messages, cancellationToken);
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
