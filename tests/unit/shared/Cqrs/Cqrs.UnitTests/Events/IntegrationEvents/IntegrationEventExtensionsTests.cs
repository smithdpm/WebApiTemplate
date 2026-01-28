using Cqrs.Events.IntegrationEvents;
using Shouldly;
using System.Text.Json;

namespace Cqrs.UnitTests.Events.IntegrationEvents;

public class IntegrationEventExtensionsTests
{
    [Fact]
    public void IntegrationEventsToOutboxMessages_ShouldConvertEventsToMessages()
    {
        // Arrange
        var event1 = new TestIntegrationEvent();
        var event2 = new TestIntegrationEvent();
        
        var eventsDict = new Dictionary<string, List<IntegrationEventBase>>
        {
            ["topic1"] = new List<IntegrationEventBase> { event1, event2 }
        };

        // Act
        var outboxMessages = IntegrationEventExtensions.IntegrationEventsToOutboxMessages(eventsDict);

        // Assert
        outboxMessages.Count.ShouldBe(2);
        outboxMessages.All(m => m.EventType == nameof(TestIntegrationEvent)).ShouldBeTrue();
        outboxMessages.All(m => m.Destination == "topic1").ShouldBeTrue();
        outboxMessages.All(m => m.Payload != null).ShouldBeTrue();
    }

    [Fact]
    public void IntegrationEventsToOutboxMessages_ShouldGroupByDestination()
    {
        // Arrange
        var event1 = new TestIntegrationEvent();
        var event2 = new TestIntegrationEvent();
        var event3 = new TestIntegrationEvent();
        
        var eventsDict = new Dictionary<string, List<IntegrationEventBase>>
        {
            ["topic1"] = new List<IntegrationEventBase> { event1, event2 },
            ["topic2"] = new List<IntegrationEventBase> { event3 }
        };

        // Act
        var outboxMessages = IntegrationEventExtensions.IntegrationEventsToOutboxMessages(eventsDict);

        // Assert
        outboxMessages.Count.ShouldBe(3);
        outboxMessages.Count(m => m.Destination == "topic1").ShouldBe(2);
        outboxMessages.Count(m => m.Destination == "topic2").ShouldBe(1);
    }

    [Fact]
    public void IntegrationEventsToOutboxMessages_ShouldSerializePayload()
    {
        // Arrange
        var testEvent = new TestIntegrationEvent();
        
        var eventsDict = new Dictionary<string, List<IntegrationEventBase>>
        {
            ["topic1"] = new List<IntegrationEventBase> { testEvent }
        };

        // Act
        var outboxMessages = IntegrationEventExtensions.IntegrationEventsToOutboxMessages(eventsDict);

        // Assert
        outboxMessages.Count.ShouldBe(1);
        var message = outboxMessages[0];
        
        var deserializedEvent = JsonSerializer.Deserialize<TestIntegrationEvent>(message.Payload);
        deserializedEvent.ShouldNotBeNull();
        deserializedEvent.ShouldBeEquivalentTo(testEvent);
    }

    [Fact]
    public void ToCloudEvent_CorrectlyMapsDataFromIntegrationEventBase()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent();

        // Act
        var cloudEvent = integrationEvent.ToCloudEvent();

        // Assert
        cloudEvent.ShouldNotBeNull();
        cloudEvent.Source.ShouldBe(AppDomain.CurrentDomain.FriendlyName);
        cloudEvent.Type.ShouldBe(nameof(TestIntegrationEvent));
        cloudEvent.Id.ShouldBe(integrationEvent.Id.ToString());
        cloudEvent.Time.ShouldBe(integrationEvent.Timestamp);
    }

    [Fact]
    public void ToCloudEvent_CorrectlyMapsSerializableData_FromIntegrationEvent()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent();

        // Act
        var cloudEvent = integrationEvent.ToCloudEvent();

        // Assert
        cloudEvent.Data.ShouldNotBeNull();
        
        var json = cloudEvent.Data.ToString();
        var deserializedEvent = JsonSerializer.Deserialize<TestIntegrationEvent>(json);
        
        deserializedEvent.ShouldNotBeNull();
        deserializedEvent.ShouldBeEquivalentTo(integrationEvent);
    }

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid AggregateRootId { get; init; } = Guid.NewGuid();

        public string Name { get; init; } = $"Event{Random.Shared.NextInt64(100).ToString()}";
        public DateTime DateTime { get; init;} = DateTime.UtcNow;

        public double Amount { get; init;} = Random.Shared.NextDouble();

        public record NestedData
        {
            public Guid NestedId { get; init; } = Guid.NewGuid();
            public string Info { get; init; } = Random.Shared.GetHexString(10);
            public int Count { get; init; } = (int)Random.Shared.NextInt64(1, 100);
        }
    }
}