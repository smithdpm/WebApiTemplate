using Ardalis.Result;
using Cqrs.Decorators;
using Cqrs.Events.IntegrationEvents;
using NSubstitute;
using Shouldly;

namespace Cqrs.UnitTests.Decorators;

public class IntegrationEventHandlerDecoratorTests
{
    [Fact]
    public async Task HandleInner_ShouldCallInnerHandler()
    {
        // Arrange
        var innerHandler = Substitute.For<IIntegrationEventHandler<TestIntegrationEvent>>();
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        innerHandler.HandleAsync(integrationEvent, cancellationToken)
            .Returns(Result.Success());
        
        var decorator = new TestIntegrationEventHandlerDecorator<TestIntegrationEvent>(innerHandler);

        // Act
        var result = await decorator.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await innerHandler.Received(1).HandleAsync(integrationEvent, cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleCorrectEventType()
    {
        // Arrange
        var innerHandler = Substitute.For<IIntegrationEventHandler<TestIntegrationEvent>>();
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        innerHandler.HandleAsync(integrationEvent, cancellationToken)
            .Returns(Result.Success());
        
        var decorator = new TestIntegrationEventHandlerDecorator<TestIntegrationEvent>(innerHandler);

        // Act
        var result = await ((IIntegrationEventHandler)decorator).HandleAsync(integrationEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await innerHandler.Received(1).HandleAsync(integrationEvent, cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenWrongEventType()
    {
        // Arrange
        var innerHandler = Substitute.For<IIntegrationEventHandler<TestIntegrationEvent>>();
        var wrongEvent = new AnotherTestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        var decorator = new TestIntegrationEventHandlerDecorator<TestIntegrationEvent>(innerHandler);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await ((IIntegrationEventHandler)decorator).HandleAsync(wrongEvent, cancellationToken));
    }

    [Fact]
    public void EventType_ShouldReturnCorrectType()
    {
        // Arrange
        var innerHandler = Substitute.For<IIntegrationEventHandler<TestIntegrationEvent>>();
        var decorator = new TestIntegrationEventHandlerDecorator<TestIntegrationEvent>(innerHandler);

        // Act
        var eventType = decorator.EventType;

        // Assert
        eventType.ShouldBe(typeof(TestIntegrationEvent));
    }

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
    }

    public record AnotherTestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
    }

    public class TestIntegrationEventHandlerDecorator<TEvent> : IntegrationEventHandlerDecorator<TEvent>
        where TEvent : IIntegrationEvent
    {
        public TestIntegrationEventHandlerDecorator(IIntegrationEventHandler<TEvent> innerHandler) 
            : base(innerHandler)
        {
        }

        public override Task<Result> HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken)
        {
            return HandleInner(integrationEvent, cancellationToken);
        }
    }
}