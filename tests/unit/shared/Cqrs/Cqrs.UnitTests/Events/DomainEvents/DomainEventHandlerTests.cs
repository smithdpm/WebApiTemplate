using Cqrs.Events.DomainEvents;
using SharedKernel.Events;
using Shouldly;

namespace Cqrs.UnitTests.Events.DomainEvents;

public class DomainEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldCallHandle_WhenCorrectEventType()
    {
        // Arrange
        var handler = new TestDomainEventHandler();
        var domainEvent = new TestDomainEvent { EntityId = Guid.NewGuid(), Name = "Test" };
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await handler.HandleAsync(domainEvent as IDomainEvent, cancellationToken);

        // Assert
        handler.HandledEvent.ShouldBe(domainEvent);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotCallHandle_WhenWrongEventType()
    {
        // Arrange
        var handler = new TestDomainEventHandler();
        var wrongEvent = new AnotherTestDomainEvent { EntityId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => 
            handler.HandleAsync(wrongEvent as IDomainEvent, cancellationToken));
        
        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentException>();
        exception.Message.ShouldContain($"Expected {nameof(TestDomainEvent)}");
        exception.Message.ShouldContain($"received {nameof(AnotherTestDomainEvent)}");
    }

    [Fact]
    public void EventType_ShouldReturnCorrectType()
    {
        // Arrange
        var handler = new TestDomainEventHandler();

        // Act
        var eventType = handler.EventType;

        // Assert
        eventType.ShouldBe(typeof(TestDomainEvent));
    }

    public class TestDomainEventHandler : DomainEventHandler<TestDomainEvent>
    {
        public TestDomainEvent? HandledEvent { get; private set; }

        public override Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            HandledEvent = domainEvent;
            return Task.CompletedTask;
        }
    }

    public record TestDomainEvent : IDomainEvent
    {
        public Guid EntityId { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public record AnotherTestDomainEvent : IDomainEvent
    {
        public Guid EntityId { get; init; }
    }
}