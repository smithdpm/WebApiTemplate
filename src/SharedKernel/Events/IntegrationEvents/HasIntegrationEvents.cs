

using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel.Events.IntegrationEvents;
public class HasIntegrationEvents : IHasIntegrationEvents
{
    [NotMapped]
    public IReadOnlyDictionary<string, List<IntegrationEventBase>> IntegrationEventsToSend => _integrationEvents.AsReadOnly();

    private Dictionary<string, List<IntegrationEventBase>> _integrationEvents = new();

    public void AddIntegrationEvent(string destination, IntegrationEventBase eventItem)
    {
        if (!_integrationEvents.ContainsKey(destination))
            _integrationEvents[destination] = new List<IntegrationEventBase>();
        _integrationEvents[destination].Add(eventItem);
    }

    public void ClearIntegrationEvents() => _integrationEvents.Clear();
}
