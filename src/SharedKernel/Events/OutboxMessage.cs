
using SharedKernel.Database;

namespace SharedKernel.Events;
public class OutboxMessage: IAggregateRoot
{
    public int Id { get; set; }
    public string EventType { get; internal set; }
    public string Payload { get; internal set; }
    public string? Destination { get; internal set; }
    public DateTimeOffset OccurredOnUtc { get; internal set; }
    public DateTimeOffset? ProcessedAtUtc { get; internal set; } = null;
    public int ProcessingAttempts { get; internal set; } = 0;
    public string? Error { get; internal set; } = null;
    public DateTimeOffset? LockedUntilUtc { get; set; } = null;

    public OutboxMessage(string eventType, string payload, DateTimeOffset occurredOnUtc, string? destination = null)
    {
        EventType = eventType;
        Payload = payload;
        OccurredOnUtc = occurredOnUtc;
        Destination = destination;
    }
}
