
namespace Cqrs.Events.IntegrationEvents;
public interface IIntegrationEventHandler<TEvent>: IIntegrationEventHandler
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
public interface IIntegrationEventHandler
{
    Type EventType { get; }
    Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
