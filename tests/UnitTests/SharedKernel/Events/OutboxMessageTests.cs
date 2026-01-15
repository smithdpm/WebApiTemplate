using Cqrs.Database;
using Shouldly;

namespace UnitTests.SharedKernel.Events;

public class OutboxMessageTests
{
    [Fact]
    public void Constructor_ShouldSetProperties_WhenValidParametersProvided()
    {
        // Arrange
        var eventType = "CarSoldEvent";
        var payload = "{\"id\":\"123\",\"price\":25000}";
        var occurredOnUtc = DateTimeOffset.UtcNow;
        var destination = "car-events-topic";

        // Act
        var outboxMessage = new OutboxMessage(eventType, payload, occurredOnUtc, destination);

        // Assert
        outboxMessage.EventType.ShouldBe(eventType);
        outboxMessage.Payload.ShouldBe(payload);
        outboxMessage.OccurredOnUtc.ShouldBe(occurredOnUtc);
        outboxMessage.Destination.ShouldBe(destination);
        outboxMessage.ProcessedAtUtc.ShouldBeNull();
        outboxMessage.ProcessingAttempts.ShouldBe(0);
        outboxMessage.Error.ShouldBeNull();
        outboxMessage.LockedUntilUtc.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ShouldSetProperties_WhenDestinationIsNull()
    {
        // Arrange
        var eventType = "DomainEvent";
        var payload = "{\"data\":\"test\"}";
        var occurredOnUtc = DateTimeOffset.UtcNow;

        // Act
        var outboxMessage = new OutboxMessage(eventType, payload, occurredOnUtc, null);

        // Assert
        outboxMessage.EventType.ShouldBe(eventType);
        outboxMessage.Payload.ShouldBe(payload);
        outboxMessage.OccurredOnUtc.ShouldBe(occurredOnUtc);
        outboxMessage.Destination.ShouldBeNull();
        outboxMessage.ProcessedAtUtc.ShouldBeNull();
        outboxMessage.ProcessingAttempts.ShouldBe(0);
        outboxMessage.Error.ShouldBeNull();
        outboxMessage.LockedUntilUtc.ShouldBeNull();
    }
}