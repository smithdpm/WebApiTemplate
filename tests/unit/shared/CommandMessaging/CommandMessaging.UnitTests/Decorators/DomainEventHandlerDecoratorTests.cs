using Ardalis.Result;
using Cqrs.Decorators;
using Cqrs.Events.DomainEvents;
using NSubstitute;
using SharedKernel.Events;
using Shouldly;

namespace Cqrs.UnitTests.Decorators;

public class DomainEventHandlerDecoratorTests
{
    [Fact]
    public async Task HandleInner_ShouldCallInnerHandler()
    {
        // Arrange
        var innerHandler = Substitute.For<IDomainEventHandler<TestDomainEvent>>();
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        innerHandler.HandleAsync(domainEvent, cancellationToken)
            .Returns(Result.Success());
        
        var decorator = new TestDomainEventHandlerDecorator<TestDomainEvent>(innerHandler);

        // Act
        var result = await decorator.HandleAsync(domainEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await innerHandler.Received(1).HandleAsync(domainEvent, cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleCorrectEventType()
    {
        // Arrange
        var innerHandler = Substitute.For<IDomainEventHandler<TestDomainEvent>>();
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        innerHandler.HandleAsync(domainEvent, cancellationToken)
            .Returns(Result.Success());
        
        var decorator = new TestDomainEventHandlerDecorator<TestDomainEvent>(innerHandler);

        // Act
        var result = await ((IDomainEventHandler)decorator).HandleAsync(domainEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await innerHandler.Received(1).HandleAsync(domainEvent, cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenWrongEventType()
    {
        // Arrange
        var innerHandler = Substitute.For<IDomainEventHandler<TestDomainEvent>>();
        var wrongEvent = new AnotherTestDomainEvent { Id = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        var decorator = new TestDomainEventHandlerDecorator<TestDomainEvent>(innerHandler);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await ((IDomainEventHandler)decorator).HandleAsync(wrongEvent, cancellationToken));
    }

    [Fact]
    public void EventType_ShouldReturnCorrectType()
    {
        // Arrange
        var innerHandler = Substitute.For<IDomainEventHandler<TestDomainEvent>>();
        var decorator = new TestDomainEventHandlerDecorator<TestDomainEvent>(innerHandler);

        // Act
        var eventType = decorator.EventType;

        // Assert
        eventType.ShouldBe(typeof(TestDomainEvent));
    }

    public class TestDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
    }

    public class AnotherTestDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
    }

    public class TestDomainEventHandlerDecorator<TEvent> : DomainEventHandlerDecorator<TEvent>
        where TEvent : IDomainEvent
    {
        public TestDomainEventHandlerDecorator(IDomainEventHandler<TEvent> innerHandler) 
            : base(innerHandler)
        {
        }

        public override Task<Result> HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)
        {
            return HandleInner(domainEvent, cancellationToken);
        }
    }
}