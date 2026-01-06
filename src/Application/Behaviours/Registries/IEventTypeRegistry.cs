
using System.Reflection;

namespace Application.Behaviours.Registries;
public interface IEventTypeRegistry
{
    Type? GetTypeByName(string eventName);
    void RegisterEventsFromAssembly(Assembly assemblyToScan, Type type);
}
