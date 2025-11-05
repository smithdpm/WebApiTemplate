
using Application.Abstractions.Events;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using SharedKernel.Events.IntegrationEvents;

namespace Infrastructure.Events;
internal class ServiceBusEventDispatcher(IAzureClientFactory<ServiceBusSender> senderFactory) : IIntegrationEventDispatcher
{
    public Task DispatchEventsAsync(IEnumerable<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken = default)
    {
        var sender = senderFactory.CreateClient("cars-events");

        sender.SendMessagesAsync(new ServiceBusMessageBatch(), cancellationToken);
        throw new NotImplementedException();
    }
}
