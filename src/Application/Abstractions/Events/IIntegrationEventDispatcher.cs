using SharedKernel.Events.IntegrationEvents;

namespace Application.Abstractions.Events;
public interface IIntegrationEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IntegrationEventBase> integrationEvents, string queueOrTopic, CancellationToken cancellationToken = default);
}
