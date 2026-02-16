using Ardalis.Result;
using Cqrs.Decorators.LoggingDecorator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;

namespace Cqrs.UnitTests.Decorators.LoggingDecorator;

public class LoggingBehaviourTests
{
    private readonly FakeLogger<LoggingBehaviour> _fakeLogger;
    private readonly LoggingBehaviour _behaviour;

    public LoggingBehaviourTests()
    {
        _fakeLogger = new FakeLogger<LoggingBehaviour>();
        _behaviour = new LoggingBehaviour(_fakeLogger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogInformation_WhenOperationSucceeds()
    {
        // Arrange
        var operationName = "TestOperation";
        var expectedResult = Result<string>.Success("Success");
        
        Task<Result<string>> action() => Task.FromResult(expectedResult);

        // Act
        var result = await _behaviour.ExecuteAsync(action, operationName);

        // Assert
        result.ShouldBe(expectedResult);
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.Count.ShouldBe(2);
        
        logs[0].Level.ShouldBe(LogLevel.Information);
        logs[0].Message.ShouldContain("Handling operation");
        logs[0].Message.ShouldContain(operationName);
        
        logs[1].Level.ShouldBe(LogLevel.Information);
        logs[1].Message.ShouldContain("handled successfully");
        logs[1].Message.ShouldContain(operationName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogError_WhenOperationFails()
    {
        // Arrange
        var operationName = "TestOperation";
        var errorMessage = "Operation failed";
        var expectedResult = Result<string>.Error(errorMessage);
        
        Task<Result<string>> action() => Task.FromResult(expectedResult);

        // Act
        var result = await _behaviour.ExecuteAsync(action, operationName);

        // Assert
        result.ShouldBe(expectedResult);
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.Count.ShouldBe(2);
        
        logs[0].Level.ShouldBe(LogLevel.Information);
        logs[0].Message.ShouldContain("Handling operation");
        
        logs[1].Level.ShouldBe(LogLevel.Error);
        logs[1].Message.ShouldContain("completed with error");
        logs[1].Message.ShouldContain(operationName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOriginalResult_WhenSuccess()
    {
        // Arrange
        var operationName = "TestOperation";
        var expectedValue = "Expected Value";
        var expectedResult = Result<string>.Success(expectedValue);
        
        Task<Result<string>> action() => Task.FromResult(expectedResult);

        // Act
        var result = await _behaviour.ExecuteAsync(action, operationName);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedValue);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOriginalResult_WhenError()
    {
        // Arrange
        var operationName = "TestOperation";
        var errorMessage = "Expected Error";
        var expectedResult = Result<string>.Error(errorMessage);
        
        Task<Result<string>> action() => Task.FromResult(expectedResult);

        // Act
        var result = await _behaviour.ExecuteAsync(action, operationName);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(e => e == errorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleResultWithoutValue()
    {
        // Arrange
        var operationName = "TestOperation";
        var expectedResult = Result.Success();
        
        Task<Result> action() => Task.FromResult(expectedResult);

        // Act
        var result = await _behaviour.ExecuteAsync(action, operationName);

        // Assert
        result.ShouldBe(expectedResult);
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Message.Contains("handled successfully"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogBeforeAndAfterExecution()
    {
        // Arrange
        var operationName = "TestOperation";
        var expectedResult = Result<string>.Success("Success");
        var executionOrder = new List<string>();
        
        Task<Result<string>> action()
        {
            executionOrder.Add("action");
            return Task.FromResult(expectedResult);
        }

        // Act
        await _behaviour.ExecuteAsync(action, operationName);

        // Assert
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.Count.ShouldBe(2);
        
        // First log should be before action
        logs[0].Message.ShouldContain("Handling operation");
        
        // Action should have been executed
        executionOrder.ShouldContain("action");
        
        // Second log should be after action
        logs[1].Message.ShouldContain("handled successfully");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateException_WhenActionThrows()
    {
        // Arrange
        var operationName = "TestOperation";
        var expectedException = new InvalidOperationException("Test exception");
        
        Task<Result<string>> action() => throw expectedException;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _behaviour.ExecuteAsync(action, operationName));
        
        exception.ShouldBe(expectedException);
        
        // Should still have the initial log
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.Count.ShouldBe(1);
        logs[0].Message.ShouldContain("Handling operation");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeOperationNameInAllLogs()
    {
        // Arrange
        var operationName = "VerySpecificOperationName";
        var expectedResult = Result<string>.Success("Success");
        
        Task<Result<string>> action() => Task.FromResult(expectedResult);

        // Act
        await _behaviour.ExecuteAsync(action, operationName);

        // Assert
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldAllBe(log => log.Message.Contains(operationName));
    }
}