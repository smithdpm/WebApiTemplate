
using SharedKernel.Events.IntegrationEvents;
using System.Reflection;

namespace Application.Behaviours.Registries;
internal class IntegrationEventTypeRegistry : IIntegrationEventTypeRegistry
{
    private readonly IReadOnlyDictionary<string, Type> _registeredEvents;
    public IntegrationEventTypeRegistry(Assembly assemblyToScan)
    {
        _registeredEvents = assemblyToScan.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IIntegrationEvent)) && !t.IsAbstract && !t.IsInterface)
            .ToDictionary(t => t.Name, t => t);
    }

    public Type? GetTypeByName(string eventName)
    {
        return _registeredEvents.GetValueOrDefault(eventName);
    }
}
