
namespace Cqrs.Events.IntegrationEvents;

public interface IHasIntegrationEvents
{
    public IReadOnlyDictionary<string, List<IntegrationEventBase>> IntegrationEventsToSend { get; }

    void AddIntegrationEvent(string destination, IntegrationEventBase eventItem);
    void ClearIntegrationEvents();
}