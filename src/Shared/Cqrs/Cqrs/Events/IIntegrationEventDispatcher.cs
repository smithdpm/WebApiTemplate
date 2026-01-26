using Cqrs.Events.IntegrationEvents;

namespace Cqrs.Abstractions.Events;
public interface IIntegrationEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IntegrationEventBase> integrationEvents, string queueOrTopic, CancellationToken cancellationToken = default);
}
