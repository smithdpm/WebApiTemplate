
using System.Reflection;

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

    public void RegisterEventsFromAssembly(Assembly assemblyToScan, Type type)
    {
        var eventsToAdd = assemblyToScan.GetTypes()
            .Where(t => t.IsAssignableTo(type) && !t.IsAbstract && !t.IsInterface)
            .ToDictionary(t => t.Name, t => t);

        foreach (var evt in eventsToAdd)
        {
            _registeredEvents.Add(evt.Key, evt.Value);
        }
    }
}
