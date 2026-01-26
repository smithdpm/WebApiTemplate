
namespace Cqrs.Events.IntegrationEvents;
public abstract record IntegrationEventBase: IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

}
