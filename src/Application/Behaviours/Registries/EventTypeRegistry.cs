

using SharedKernel.Events.DomainEvents;
using SharedKernel.Events.IntegrationEvents;

namespace Application.Behaviours.Registries;
internal class EventTypeRegistry : IEventTypeRegistry
{
    private Dictionary<string, Type> _registeredEvents;

    public EventTypeRegistry()
    {
        _registeredEvents = new Dictionary<string, Type>();
    }

    public Type? GetTypeByName(string eventName)
    {
        return _registeredEvents.GetValueOrDefault(eventName);
    }

    public void RegisterIntegrationEventsFromAssemblyTypes(Type[] assemblyTypesToScan)
    {
        var eventsToAdd = ExtractEventTypesToDictionary(assemblyTypesToScan, typeof(IIntegrationEvent));

        foreach (var evt in eventsToAdd)
        {
            _registeredEvents.Add(evt.Key, evt.Value);
        }
    }
    public void RegisterDomainEventsFromAssemblyTypes(Type[] assemblyTypesToScan)
    {
        var eventsToAdd = ExtractEventTypesToDictionary(assemblyTypesToScan, typeof(IDomainEvent));

        foreach (var evt in eventsToAdd)
        {
            _registeredEvents.Add(evt.Key, evt.Value);
        }
    }

    private Dictionary<string, Type> ExtractEventTypesToDictionary(Type[] assemblyTypesToScan, Type type)
    {
        return assemblyTypesToScan
            .Where(t => t.IsAssignableTo(type) && !t.IsAbstract && !t.IsInterface)
            .ToDictionary(t => t.Name, t => t);
    }

}
