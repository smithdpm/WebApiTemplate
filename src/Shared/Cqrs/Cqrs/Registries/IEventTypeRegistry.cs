

namespace Cqrs.Decorators.Registries;
public interface IEventTypeRegistry
{
    Type? GetTypeByName(string eventName);
    void RegisterIntegrationEventsFromAssemblyTypes(Type[] assemblyTypesToScan);
    void RegisterDomainEventsFromAssemblyTypes(Type[] assemblyTypesToScan);
}
