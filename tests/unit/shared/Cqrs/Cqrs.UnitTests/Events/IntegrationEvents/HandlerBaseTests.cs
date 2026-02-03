using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Messaging;
using Shouldly;

namespace Cqrs.UnitTests.Events.IntegrationEvents;

public class HandlerBaseTests
{
    private readonly TestHandler _testHandler;

    public HandlerBaseTests()
    {
        _testHandler = new TestHandler();
    }

    [Fact]
    public void AddIntegrationEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var destination = "test-topic";
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test-data" };

        // Act
        _testHandler.AddIntegrationEvent(integrationEvent, destination);

        // Assert
        _testHandler.IntegrationEventsToSend.ShouldContainKey(destination);
        _testHandler.IntegrationEventsToSend[destination].Count.ShouldBe(1);
        _testHandler.IntegrationEventsToSend[destination][0].ShouldBe(integrationEvent);
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
        _testHandler.AddIntegrationEvent(event1, destination1);
        _testHandler.AddIntegrationEvent(event2, destination2);
        _testHandler.AddIntegrationEvent(event3, destination1);

        // Assert
        var events = _testHandler.IntegrationEventsToSend;
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
        _testHandler.AddIntegrationEvent(integrationEvent, destination);

        // Act
        _testHandler.ClearIntegrationEvents();

        // Assert
        _testHandler.IntegrationEventsToSend.ShouldBeEmpty();
    }

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }

    public class TestHandler : HandlerBase<string, Result<string>>
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;

        public override Task<Result<string>> HandleAsync(string input, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success("success"));
        }
    }
}