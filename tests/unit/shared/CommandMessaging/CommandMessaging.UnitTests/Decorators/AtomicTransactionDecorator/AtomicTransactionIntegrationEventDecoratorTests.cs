using Ardalis.Result;
using Cqrs.Decorators.AtomicTransactionDecorator;
using Cqrs.Events.IntegrationEvents;
using NSubstitute;
using Shouldly;

namespace Cqrs.UnitTests.Decorators.AtomicTransactionDecorator;

public class AtomicTransactionIntegrationEventDecoratorTests
{
    private readonly IIntegrationEventHandler<TestIntegrationEvent> _innerHandler;
    private readonly IAtomicTransactionBehaviour _atomicTransactionBehaviour;
    private readonly AtomicTransactionIntegrationEventDecorator<TestIntegrationEvent> _decorator;

    public AtomicTransactionIntegrationEventDecoratorTests()
    {
        _innerHandler = Substitute.For<IIntegrationEventHandler<TestIntegrationEvent>>();
        _atomicTransactionBehaviour = Substitute.For<IAtomicTransactionBehaviour>();
        _decorator = new AtomicTransactionIntegrationEventDecorator<TestIntegrationEvent>(_innerHandler, _atomicTransactionBehaviour);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallBehaviourExecuteAsync()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var expectedResult = Result.Success();
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _atomicTransactionBehaviour.ExecuteAsync(
            Arg.Any<Func<Task<Result>>>(), 
            cancellationToken)
            .Returns(expectedResult);

        // Act
        var result = await _decorator.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        result.ShouldBe(expectedResult);
        await _atomicTransactionBehaviour.Received(1).ExecuteAsync(
            Arg.Any<Func<Task<Result>>>(), 
            cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassInnerHandlerToBehaviourExecuteAsync()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var expectedResult = Result.Success();
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _innerHandler.HandleAsync(integrationEvent, cancellationToken)
            .Returns(expectedResult);
        
        _atomicTransactionBehaviour.ExecuteAsync(
            Arg.Any<Func<Task<Result>>>(), 
            cancellationToken)
            .Returns(callInfo => callInfo.ArgAt<Func<Task<Result>>>(0)());

        // Act
        await _decorator.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        await _innerHandler.Received(1).HandleAsync(integrationEvent, cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBehaviourResult()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var expectedResult = Result.Error("Modified by behaviour");
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _atomicTransactionBehaviour.ExecuteAsync(
            Arg.Any<Func<Task<Result>>>(), 
            cancellationToken)
            .Returns(expectedResult);

        // Act
        var result = await _decorator.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        result.ShouldBe(expectedResult);
        result.IsSuccess.ShouldBeFalse();
    }

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
    }
}