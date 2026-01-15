
namespace Cqrs.Events.IntegrationEvents;
public interface IIntegrationEventHandler<TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
