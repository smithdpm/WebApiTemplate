using Ardalis.Result;
using Cqrs.Decorators.IntegrationEventToOutboxDecorator;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Outbox;
using NSubstitute;
using SharedKernel.Database;
using Shouldly;

namespace Cqrs.UnitTests.Decorators.IntegrationEventToOutboxDecorator;

public class IntegrationEventToOutboxIntegrationEventDecoratorTests
{
    private readonly IRepository<OutboxMessage> _repository;
    private readonly IIntegrationEventToOutboxBehaviour _integrationEventBehaviour;

    public IntegrationEventToOutboxIntegrationEventDecoratorTests()
    {
        _repository = Substitute.For<IRepository<OutboxMessage>>();
        _integrationEventBehaviour = new IntegrationEventToOutboxBehaviour(_repository);
    }

    [Fact]
    public async Task Handle_ShouldCaptureIntegrationEvents_WhenIntegrationEventHandlingSucceeds()
    {
        // Arrange
        var innerHandler = new TestIntegrationEventHandlerWithEvents<TestIntegrationEvent>();
        var decorator = new IntegrationEventToOutboxIntegrationEventDecorator<TestIntegrationEvent>(innerHandler, _integrationEventBehaviour);
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        innerHandler.AddIntegrationEvent(new AnotherTestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" }, "topic1");
        innerHandler.SetResult(Result.Success());

        // Act
        var result = await decorator.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _repository.Received(1).AddRangeAsync(
            Arg.Is<List<OutboxMessage>>(messages => 
                messages.Count == 1 && 
                messages[0].Destination == "topic1" &&
                messages[0].EventType == nameof(AnotherTestIntegrationEvent)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotCaptureEvents_WhenIntegrationEventHandlingFails()
    {
        // Arrange
        var innerHandler = new TestIntegrationEventHandlerWithEvents<TestIntegrationEvent>();
        var decorator = new IntegrationEventToOutboxIntegrationEventDecorator<TestIntegrationEvent>(innerHandler, _integrationEventBehaviour);
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        innerHandler.AddIntegrationEvent(new AnotherTestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" }, "topic1");
        innerHandler.SetResult(Result.Error("Event handling failed"));

        // Act
        var result = await decorator.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldHandleNoEvents_WhenHandlerHasNoEventsToSend()
    {
        // Arrange
        var innerHandler = new TestIntegrationEventHandlerWithEvents<TestIntegrationEvent>();
        var decorator = new IntegrationEventToOutboxIntegrationEventDecorator<TestIntegrationEvent>(innerHandler, _integrationEventBehaviour);
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        innerHandler.SetResult(Result.Success());

        // Act
        var result = await decorator.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassThroughResult_WhenDecoratingHandler()
    {
        // Arrange
        var innerHandler = new TestIntegrationEventHandlerWithEvents<TestIntegrationEvent>();
        var decorator = new IntegrationEventToOutboxIntegrationEventDecorator<TestIntegrationEvent>(innerHandler, _integrationEventBehaviour);
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        innerHandler.SetResult(Result.Success());

        // Act
        var result = await decorator.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    private record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
    }

    private record AnotherTestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }

    private class TestIntegrationEventHandlerWithEvents<TEvent> : IntegrationEventHandler<TEvent>
        where TEvent : IIntegrationEvent
    {
        private Result _result = null!;

        public void SetResult(Result result)
        {
            _result = result;
        }

        public override Task<Result> HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }
}