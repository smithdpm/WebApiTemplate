namespace Cqrs.Events.IntegrationEvents;
public interface IIntegrationEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IntegrationEventBase> integrationEvents, string queueOrTopic, CancellationToken cancellationToken = default);
}
