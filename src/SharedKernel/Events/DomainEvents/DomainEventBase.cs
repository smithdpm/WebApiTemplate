using System.Text.Json.Serialization;

namespace SharedKernel.Events.DomainEvents;

public abstract record DomainEventBase: IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}