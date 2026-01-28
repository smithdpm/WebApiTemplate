using Cqrs.Events.IntegrationEvents;
using Shouldly;

namespace Cqrs.UnitTests.Events.IntegrationEvents;

public class IntegrationEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldCallHandle_WhenCorrectEventType()
    {
        // Arrange
        var handler = new TestIntegrationEventHandler();
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "Test" };
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await handler.HandleAsync(integrationEvent as IIntegrationEvent, cancellationToken);

        // Assert
        handler.HandledEvent.ShouldBe(integrationEvent);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotCallHandle_WhenWrongEventType()
    {
        // Arrange
        var handler = new TestIntegrationEventHandler();
        var wrongEvent = new AnotherTestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => 
            handler.HandleAsync(wrongEvent as IIntegrationEvent, cancellationToken));
        
        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentException>();
        exception.Message.ShouldContain($"Expected {nameof(TestIntegrationEvent)}");
        exception.Message.ShouldContain($"received {nameof(AnotherTestIntegrationEvent)}");
    }

    [Fact]
    public void EventType_ShouldReturnCorrectType()
    {
        // Arrange
        var handler = new TestIntegrationEventHandler();

        // Act
        var eventType = handler.EventType;

        // Assert
        eventType.ShouldBe(typeof(TestIntegrationEvent));
    }

    public class TestIntegrationEventHandler : IntegrationEventHandler<TestIntegrationEvent>
    {
        public TestIntegrationEvent? HandledEvent { get; private set; }

        public override Task HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            HandledEvent = integrationEvent;
            return Task.CompletedTask;
        }
    }

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }

    public record AnotherTestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
    }
}