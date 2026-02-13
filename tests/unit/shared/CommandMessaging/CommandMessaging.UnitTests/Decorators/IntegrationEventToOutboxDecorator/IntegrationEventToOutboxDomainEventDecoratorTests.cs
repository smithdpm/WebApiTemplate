using Ardalis.Result;
using Cqrs.Decorators.IntegrationEventToOutboxDecorator;
using Cqrs.Events.DomainEvents;
using Cqrs.Events.IntegrationEvents;
using NSubstitute;
using SharedKernel.Events;
using Shouldly;

namespace Cqrs.UnitTests.Decorators.IntegrationEventToOutboxDecorator;

public class IntegrationEventToOutboxDomainEventDecoratorTests
{
    private readonly DomainEventHandler<TestDomainEvent> _innerHandler;
    private readonly IIntegrationEventToOutboxBehaviour _integrationEventBehaviour;
    private readonly IntegrationEventToOutboxDomainEventDecorator<TestDomainEvent> _decorator;

    public IntegrationEventToOutboxDomainEventDecoratorTests()
    {
        _innerHandler = Substitute.For<DomainEventHandler<TestDomainEvent>>();
        _integrationEventBehaviour = Substitute.For<IIntegrationEventToOutboxBehaviour>();
        _decorator = new IntegrationEventToOutboxDomainEventDecorator<TestDomainEvent>(_innerHandler, _integrationEventBehaviour);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallBehaviourExecuteAsync()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var expectedResult = Result.Success();
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _integrationEventBehaviour.ExecuteAsync(_innerHandler, domainEvent, cancellationToken)
            .Returns(expectedResult);

        // Act
        var result = await _decorator.HandleAsync(domainEvent, cancellationToken);

        // Assert
        result.ShouldBe(expectedResult);
        await _integrationEventBehaviour.Received(1).ExecuteAsync(_innerHandler, domainEvent, cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCorrectParametersToBehaviour()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        DomainEventHandler<TestDomainEvent>? capturedHandler = null;
        TestDomainEvent? capturedEvent = null;
        CancellationToken capturedToken = default;
        
        _integrationEventBehaviour.ExecuteAsync(
            Arg.Do<DomainEventHandler<TestDomainEvent>>(h => capturedHandler = h),
            Arg.Do<TestDomainEvent>(e => capturedEvent = e),
            Arg.Do<CancellationToken>(t => capturedToken = t))
            .Returns(Result.Success());

        // Act
        await _decorator.HandleAsync(domainEvent, cancellationToken);

        // Assert
        capturedHandler.ShouldBe(_innerHandler);
        capturedEvent.ShouldBe(domainEvent);
        capturedToken.ShouldBe(cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBehaviourResult()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var expectedResult = Result.Error("Modified by behaviour");
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _integrationEventBehaviour.ExecuteAsync(_innerHandler, domainEvent, cancellationToken)
            .Returns(expectedResult);

        // Act
        var result = await _decorator.HandleAsync(domainEvent, cancellationToken);

        // Assert
        result.ShouldBe(expectedResult);
        result.IsSuccess.ShouldBeFalse();
    }

    public class TestDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
    }

    private record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }
}