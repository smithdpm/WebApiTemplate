using Ardalis.Result;
using Cqrs.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Microsoft.Extensions.Logging.Testing;
using Cqrs.Decorators.LoggingDecorator;

namespace Cqrs.UnitTests.Decorators;

public class LoggingDecoratorTests
{
    public class CommandHandlerWithResponseTests : LoggingDecoratorTests
    {
        private readonly FakeLogger<LoggingBehaviour> _fakeLogger;
        private readonly ICommandHandler<TestCommand, string> _innerHandler;
        private readonly LoggingCommandDecorator<TestCommand, string> _decorator;
        private readonly ILoggingBehaviour _loggingBehaviour;
        public CommandHandlerWithResponseTests()
        {
            _fakeLogger = new FakeLogger<LoggingBehaviour>();
            _innerHandler = Substitute.For<ICommandHandler<TestCommand, string>>();
            _loggingBehaviour = new LoggingBehaviour(_fakeLogger);
            _decorator = new LoggingCommandDecorator<TestCommand, string>(_innerHandler, _loggingBehaviour);
        }

        [Fact]
        public async Task Handle_ShouldLogInformation_WhenCommandSucceeds()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var expectedResult = "Success";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();

            var logs = _fakeLogger.Collector.GetSnapshot();
            Assert.All(logs, log => log.Level.ShouldBe(LogLevel.Information));
            Assert.Contains(logs, log => log.Message.Contains("Handling command"));
            Assert.Contains(logs, log => log.Message.Contains("handled successfully"));
        }

        [Fact]
        public async Task Handle_ShouldLogError_WhenCommandFails()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var errorMessage = "Command failed";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result<string>.Error(errorMessage));

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

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
            var command = new TestCommand { Id = Guid.NewGuid() };
            var expectedResult = "Expected Result";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await _innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }

        [Fact]
        public async Task Handle_ShouldLogCommandName_WhenProcessing()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result<string>.Success("Success"));

            // Act
            await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            var logs = _fakeLogger.Collector.GetSnapshot();
            logs.ShouldContain(log => log.Message.Contains(nameof(TestCommand)));
        }
    }

    public class CommandHandlerWithoutResponseTests : LoggingDecoratorTests
    {
        private readonly FakeLogger<LoggingBehaviour> _fakeLogger;
        private readonly ICommandHandler<TestVoidCommand> _innerHandler;
        private readonly LoggingBehaviour _loggingBehaviour;
        private readonly LoggingCommandDecorator<TestVoidCommand> _decorator;

        public CommandHandlerWithoutResponseTests()
        {
            _fakeLogger = new FakeLogger<LoggingBehaviour>();
            _innerHandler = Substitute.For<ICommandHandler<TestVoidCommand>>();
            _loggingBehaviour = new LoggingBehaviour(_fakeLogger);
            _decorator = new LoggingCommandDecorator<TestVoidCommand>(_innerHandler, _loggingBehaviour);
        }

        [Fact]
        public async Task Handle_ShouldLogInformation_WhenCommandSucceeds()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result.Success());

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            
            var logs = _fakeLogger.Collector.GetSnapshot();
            Assert.All(logs, log => log.Level.ShouldBe(LogLevel.Information));
            Assert.Contains(logs, log => log.Message.Contains("Handling command"));
            Assert.Contains(logs, log => log.Message.Contains("handled successfully"));
        }

        [Fact]
        public async Task Handle_ShouldLogError_WhenCommandFails()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var errorMessage = "Command failed";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result.Error(errorMessage));

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

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
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            var expectedResult = Result.Success();
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(expectedResult);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.ShouldBe(expectedResult);
            await _innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }

         [Fact]
        public async Task Handle_ShouldLogCommandName_WhenProcessing()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            var expectedResult = Result.Success();
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(expectedResult);

            // Act
            await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            var logs = _fakeLogger.Collector.GetSnapshot();
            logs.ShouldContain(log => log.Message.Contains(nameof(TestVoidCommand)));
        }
    }

    public class TestCommand : ICommand<string>
    {
        public Guid Id { get; set; }
    }

    public class TestVoidCommand : ICommand
    {
        public Guid Id { get; set; }
    }
}