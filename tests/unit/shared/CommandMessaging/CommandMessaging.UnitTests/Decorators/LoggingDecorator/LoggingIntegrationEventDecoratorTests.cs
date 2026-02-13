using Ardalis.Result;
using Cqrs.Decorators.LoggingDecorator;
using Cqrs.Events.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using Shouldly;

namespace Cqrs.UnitTests.Decorators.LoggingDecorator;

public class LoggingIntegrationEventDecoratorTests
{
    private readonly FakeLogger<LoggingBehaviour> _fakeLogger;
    private readonly IIntegrationEventHandler<TestIntegrationEvent> _innerHandler;
    private readonly LoggingIntegrationEventDecorator<TestIntegrationEvent> _decorator;
    private readonly ILoggingBehaviour _loggingBehaviour;

    public LoggingIntegrationEventDecoratorTests()
    {
        _fakeLogger = new FakeLogger<LoggingBehaviour>();
        _innerHandler = Substitute.For<IIntegrationEventHandler<TestIntegrationEvent>>();
        _loggingBehaviour = new LoggingBehaviour(_fakeLogger);
        _decorator = new LoggingIntegrationEventDecorator<TestIntegrationEvent>(_innerHandler, _loggingBehaviour);
    }

    [Fact]
    public async Task Handle_ShouldLogInformation_WhenEventHandlingSucceeds()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _innerHandler.HandleAsync(integrationEvent, cancellationToken)
            .Returns(Result.Success());

        // Act
        var result = await _decorator.HandleAsync(integrationEvent, cancellationToken);

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
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var errorMessage = "Event handling failed";
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _innerHandler.HandleAsync(integrationEvent, cancellationToken)
            .Returns(Result.Error(errorMessage));

        // Act
        var result = await _decorator.HandleAsync(integrationEvent, cancellationToken);

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
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _innerHandler.HandleAsync(integrationEvent, cancellationToken)
            .Returns(Result.Success());

        // Act
        var result = await _decorator.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _innerHandler.Received(1).HandleAsync(integrationEvent, cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldLogEventName_WhenProcessing()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _innerHandler.HandleAsync(integrationEvent, cancellationToken)
            .Returns(Result.Success());

        // Act
        await _decorator.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Message.Contains(nameof(TestIntegrationEvent)));
    }

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
    }
}