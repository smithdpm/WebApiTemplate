
namespace Application.Behaviours.Registries;
public interface IIntegrationEventTypeRegistry
{
    Type? GetTypeByName(string eventName);
}
