using Cqrs.Events.IntegrationEvents;
using Shouldly;

namespace Cqrs.UnitTests.Events.IntegrationEvents;

public class HasIntegrationEventsTests
{
    private readonly HasIntegrationEvents _hasIntegrationEvents;

    public HasIntegrationEventsTests()
    {
        _hasIntegrationEvents = new HasIntegrationEvents();
    }

    [Fact]
    public void AddIntegrationEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var destination = "test-topic";
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test-data" };

        // Act
        _hasIntegrationEvents.AddIntegrationEvent(destination, integrationEvent);

        // Assert
        _hasIntegrationEvents.IntegrationEventsToSend.ShouldContainKey(destination);
        _hasIntegrationEvents.IntegrationEventsToSend[destination].Count.ShouldBe(1);
        _hasIntegrationEvents.IntegrationEventsToSend[destination][0].ShouldBe(integrationEvent);
    }

    [Fact]
    public void IntegrationEventsToSend_ShouldReturnAllEvents()
    {
        // Arrange
        var destination1 = "topic1";
        var destination2 = "topic2";
        var event1 = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event1" };
        var event2 = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event2" };
        var event3 = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event3" };

        // Act
        _hasIntegrationEvents.AddIntegrationEvent(destination1, event1);
        _hasIntegrationEvents.AddIntegrationEvent(destination2, event2);
        _hasIntegrationEvents.AddIntegrationEvent(destination1, event3);

        // Assert
        var events = _hasIntegrationEvents.IntegrationEventsToSend;
        events.Count.ShouldBe(2);
        events[destination1].Count.ShouldBe(2);
        events[destination1].ShouldContain(event1);
        events[destination1].ShouldContain(event3);
        events[destination2].Count.ShouldBe(1);
        events[destination2].ShouldContain(event2);
    }

    [Fact]
    public void ClearIntegrationEvents_ShouldEmptyCollection()
    {
        // Arrange
        var destination = "test-topic";
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test-data" };
        _hasIntegrationEvents.AddIntegrationEvent(destination, integrationEvent);

        // Act
        _hasIntegrationEvents.ClearIntegrationEvents();

        // Assert
        _hasIntegrationEvents.IntegrationEventsToSend.ShouldBeEmpty();
    }

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }
}