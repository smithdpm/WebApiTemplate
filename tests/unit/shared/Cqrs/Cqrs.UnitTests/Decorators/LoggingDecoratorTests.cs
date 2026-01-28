using Ardalis.Result;
using Cqrs.Decorators;
using Cqrs.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Microsoft.Extensions.Logging.Testing;

namespace Cqrs.UnitTests.Decorators;

public class LoggingDecoratorTests
{
    public class CommandHandlerWithResponseTests : LoggingDecoratorTests
    {
        private readonly FakeLogger<LoggingDecorator<TestCommand, string>> _fakeLogger;
        private readonly ICommandHandler<TestCommand, string> _innerHandler;
        private readonly LoggingDecorator<TestCommand, string> _decorator;

        public CommandHandlerWithResponseTests()
        {
            _fakeLogger = new FakeLogger<LoggingDecorator<TestCommand, string>>();
            _innerHandler = Substitute.For<ICommandHandler<TestCommand, string>>();
            _decorator = new LoggingDecorator<TestCommand, string>(_innerHandler, _fakeLogger);
        }

        [Fact]
        public async Task Handle_ShouldLogInformation_WhenCommandSucceeds()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var expectedResult = "Success";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));

            // Act
            var result = await _decorator.Handle(command, cancellationToken);

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
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(Result<string>.Error(errorMessage));

            // Act
            var result = await _decorator.Handle(command, cancellationToken);

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
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));

            // Act
            var result = await _decorator.Handle(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await _innerHandler.Received(1).Handle(command, cancellationToken);
        }

        [Fact]
        public async Task Handle_ShouldLogCommandName_WhenProcessing()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(Result<string>.Success("Success"));

            // Act
            await _decorator.Handle(command, cancellationToken);

            // Assert
            var logs = _fakeLogger.Collector.GetSnapshot();
            logs.ShouldContain(log => log.Message.Contains(nameof(TestCommand)));
        }
    }

    public class CommandHandlerWithoutResponseTests : LoggingDecoratorTests
    {
        private readonly FakeLogger<LoggingDecorator<TestVoidCommand>> _fakeLogger;
        private readonly ICommandHandler<TestVoidCommand> _innerHandler;
        private readonly LoggingDecorator<TestVoidCommand> _decorator;

        public CommandHandlerWithoutResponseTests()
        {
            _fakeLogger = new FakeLogger<LoggingDecorator<TestVoidCommand>>();
            _innerHandler = Substitute.For<ICommandHandler<TestVoidCommand>>();
            _decorator = new LoggingDecorator<TestVoidCommand>(_innerHandler, _fakeLogger);
        }

        [Fact]
        public async Task Handle_ShouldLogInformation_WhenCommandSucceeds()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(Result.Success());

            // Act
            var result = await _decorator.Handle(command, cancellationToken);

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
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(Result.Error(errorMessage));

            // Act
            var result = await _decorator.Handle(command, cancellationToken);

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
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(expectedResult);

            // Act
            var result = await _decorator.Handle(command, cancellationToken);

            // Assert
            result.ShouldBe(expectedResult);
            await _innerHandler.Received(1).Handle(command, cancellationToken);
        }

         [Fact]
        public async Task Handle_ShouldLogCommandName_WhenProcessing()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            var expectedResult = Result.Success();
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(expectedResult);

            // Act
            await _decorator.Handle(command, cancellationToken);

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