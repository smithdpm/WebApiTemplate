using Ardalis.Result;
using Cqrs.Decorators.LoggingDecorator;
using Cqrs.Events.DomainEvents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using SharedKernel.Events;
using Shouldly;

namespace Cqrs.UnitTests.Decorators.LoggingDecorator;

public class LoggingDomainEventDecoratorTests
{
    private readonly FakeLogger<LoggingBehaviour> _fakeLogger;
    private readonly IDomainEventHandler<TestDomainEvent> _innerHandler;
    private readonly LoggingDomainEventDecorator<TestDomainEvent> _decorator;
    private readonly ILoggingBehaviour _loggingBehaviour;

    public LoggingDomainEventDecoratorTests()
    {
        _fakeLogger = new FakeLogger<LoggingBehaviour>();
        _innerHandler = Substitute.For<IDomainEventHandler<TestDomainEvent>>();
        _loggingBehaviour = new LoggingBehaviour(_fakeLogger);
        _decorator = new LoggingDomainEventDecorator<TestDomainEvent>(_innerHandler, _loggingBehaviour);
    }

    [Fact]
    public async Task Handle_ShouldLogInformation_WhenEventHandlingSucceeds()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _innerHandler.HandleAsync(domainEvent, cancellationToken)
            .Returns(Result.Success());

        // Act
        var result = await _decorator.HandleAsync(domainEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var logs = _fakeLogger.Collector.GetSnapshot();
        Assert.All(logs, log => log.Level.ShouldBe(LogLevel.Information));
        Assert.Contains(logs, log => log.Message.Contains("Handling operation"));
        Assert.Contains(logs, log => log.Message.Contains("handled successfully"));
    }

    [Fact]
    public async Task Handle_ShouldLogError_WhenEventHandlingFails()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var errorMessage = "Event handling failed";
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _innerHandler.HandleAsync(domainEvent, cancellationToken)
            .Returns(Result.Error(errorMessage));

        // Act
        var result = await _decorator.HandleAsync(domainEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();

        var lastLog = _fakeLogger.Collector.LatestRecord;
        lastLog.Level.ShouldBe(LogLevel.Error);
        lastLog.Message.ShouldContain("completed with error");
    }

    [Fact]
    public async Task Handle_ShouldPassThroughResult_WhenDecoratingHandler()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _innerHandler.HandleAsync(domainEvent, cancellationToken)
            .Returns(Result.Success());

        // Act
        var result = await _decorator.HandleAsync(domainEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _innerHandler.Received(1).HandleAsync(domainEvent, cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldLogEventName_WhenProcessing()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _innerHandler.HandleAsync(domainEvent, cancellationToken)
            .Returns(Result.Success());

        // Act
        await _decorator.HandleAsync(domainEvent, cancellationToken);

        // Assert
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Message.Contains(nameof(TestDomainEvent)));
    }

    public class TestDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
    }
}