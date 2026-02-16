
namespace Cqrs.Events.IntegrationEvents;
public abstract record IntegrationEventBase: IIntegrationEvent
{
    public Guid Id { get; init;} = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

}
