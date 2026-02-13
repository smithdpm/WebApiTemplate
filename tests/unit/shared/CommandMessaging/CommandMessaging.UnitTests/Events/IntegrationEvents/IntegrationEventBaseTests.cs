using Cqrs.Events.IntegrationEvents;
using Shouldly;

namespace Cqrs.UnitTests.Events.IntegrationEvents;

public class IntegrationEventBaseTests
{
    [Fact]
    public void Constructor_ShouldSetTimestamp()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var integrationEvent = new TestIntegrationEvent();

        // Assert
        var afterCreation = DateTimeOffset.UtcNow;
        integrationEvent.Timestamp.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        integrationEvent.Timestamp.ShouldBeLessThanOrEqualTo(afterCreation);
    }

    [Fact]
    public void Constructor_ShouldSetId()
    {
        // Arrange & Act
        var event1 = new TestIntegrationEvent();
        var event2 = new TestIntegrationEvent();

        // Assert
        event1.Id.ShouldNotBe(Guid.Empty);
        event2.Id.ShouldNotBe(Guid.Empty);
        event1.Id.ShouldNotBe(event2.Id);
    }

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public string Data { get; init; } = string.Empty;
    }
}