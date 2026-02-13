using Ardalis.Result;
using Cqrs.Decorators.AtomicTransactionDecorator;
using Cqrs.Events.DomainEvents;
using NSubstitute;
using SharedKernel.Events;
using Shouldly;

namespace Cqrs.UnitTests.Decorators.AtomicTransactionDecorator;

public class AtomicTransactionDomainEventDecoratorTests
{
    private readonly IDomainEventHandler<TestDomainEvent> _innerHandler;
    private readonly IAtomicTransactionBehaviour _atomicTransactionBehaviour;
    private readonly AtomicTransactionDomainEventDecorator<TestDomainEvent> _decorator;

    public AtomicTransactionDomainEventDecoratorTests()
    {
        _innerHandler = Substitute.For<IDomainEventHandler<TestDomainEvent>>();
        _atomicTransactionBehaviour = Substitute.For<IAtomicTransactionBehaviour>();
        _decorator = new AtomicTransactionDomainEventDecorator<TestDomainEvent>(_innerHandler, _atomicTransactionBehaviour);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallBehaviourExecuteAsync()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var expectedResult = Result.Success();
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _atomicTransactionBehaviour.ExecuteAsync(
            Arg.Any<Func<Task<Result>>>(), 
            cancellationToken)
            .Returns(expectedResult);

        // Act
        var result = await _decorator.HandleAsync(domainEvent, cancellationToken);

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
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var expectedResult = Result.Success();
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _innerHandler.HandleAsync(domainEvent, cancellationToken)
            .Returns(expectedResult);
        
        _atomicTransactionBehaviour.ExecuteAsync(
            Arg.Any<Func<Task<Result>>>(), 
            cancellationToken)
            .Returns(callInfo => callInfo.ArgAt<Func<Task<Result>>>(0)());

        // Act
        await _decorator.HandleAsync(domainEvent, cancellationToken);

        // Assert
        await _innerHandler.Received(1).HandleAsync(domainEvent, cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBehaviourResult()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var expectedResult = Result.Error("Modified by behaviour");
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _atomicTransactionBehaviour.ExecuteAsync(
            Arg.Any<Func<Task<Result>>>(), 
            cancellationToken)
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
}