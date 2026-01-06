namespace SharedKernel.Events.DomainEvents;

public abstract record DomainEventBase: IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}