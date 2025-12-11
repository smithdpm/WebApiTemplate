
namespace SharedKernel.Events.IntegrationEvents;
public abstract record IntegrationEventBase: IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; } = DateTime.UtcNow;
}
