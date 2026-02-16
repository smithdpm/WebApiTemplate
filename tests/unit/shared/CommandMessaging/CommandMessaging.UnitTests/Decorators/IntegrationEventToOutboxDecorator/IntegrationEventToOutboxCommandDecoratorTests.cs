using Ardalis.Result;
using Cqrs.Decorators.IntegrationEventToOutboxDecorator;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Operations.Commands;
using NSubstitute;
using Shouldly;

namespace Cqrs.UnitTests.Decorators.IntegrationEventToOutboxDecorator;

public class IntegrationEventToOutboxCommandDecoratorTests
{
    public class CommandHandlerWithResponseTests : IntegrationEventToOutboxCommandDecoratorTests
    {
        private readonly CommandHandler<TestCommand, string> _innerHandler;
        private readonly IIntegrationEventToOutboxBehaviour _integrationEventBehaviour;
        private readonly IntegrationEventToOutboxCommandDecorator<TestCommand, string> _decorator;

        public CommandHandlerWithResponseTests()
        {
            _innerHandler = Substitute.For<CommandHandler<TestCommand, string>>();
            _integrationEventBehaviour = Substitute.For<IIntegrationEventToOutboxBehaviour>();
            _decorator = new IntegrationEventToOutboxCommandDecorator<TestCommand, string>(_innerHandler, _integrationEventBehaviour);
        }

        [Fact]
        public async Task HandleAsync_ShouldCallBehaviourExecuteAsync()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var expectedResult = Result<string>.Success("Success");
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _integrationEventBehaviour.ExecuteAsync(_innerHandler, command, cancellationToken)
                .Returns(expectedResult);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.ShouldBe(expectedResult);
            await _integrationEventBehaviour.Received(1).ExecuteAsync(_innerHandler, command, cancellationToken);
        }

        [Fact]
        public async Task HandleAsync_ShouldPassCorrectParametersToBehaviour()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            CommandHandler<TestCommand, string>? capturedHandler = null;
            TestCommand? capturedCommand = null;
            CancellationToken capturedToken = default;
            
            _integrationEventBehaviour.ExecuteAsync(
                Arg.Do<CommandHandler<TestCommand, string>>(h => capturedHandler = h),
                Arg.Do<TestCommand>(c => capturedCommand = c),
                Arg.Do<CancellationToken>(t => capturedToken = t))
                .Returns(Result<string>.Success("Success"));

            // Act
            await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            capturedHandler.ShouldBe(_innerHandler);
            capturedCommand.ShouldBe(command);
            capturedToken.ShouldBe(cancellationToken);
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnBehaviourResult()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var expectedResult = Result<string>.Success("Modified by behaviour");
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _integrationEventBehaviour.ExecuteAsync(_innerHandler, command, cancellationToken)
                .Returns(expectedResult);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.ShouldBe(expectedResult);
            result.Value.ShouldBe("Modified by behaviour");
        }
    }

    public class CommandHandlerWithoutResponseTests : IntegrationEventToOutboxCommandDecoratorTests
    {
        private readonly CommandHandler<TestVoidCommand> _innerHandler;
        private readonly IIntegrationEventToOutboxBehaviour _integrationEventBehaviour;
        private readonly IntegrationEventToOutboxCommandDecorator<TestVoidCommand> _decorator;

        public CommandHandlerWithoutResponseTests()
        {
            _innerHandler = Substitute.For<CommandHandler<TestVoidCommand>>();
            _integrationEventBehaviour = Substitute.For<IIntegrationEventToOutboxBehaviour>();
            _decorator = new IntegrationEventToOutboxCommandDecorator<TestVoidCommand>(_innerHandler, _integrationEventBehaviour);
        }

        [Fact]
        public async Task HandleAsync_ShouldCallBehaviourExecuteAsync()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var expectedResult = Result.Success();
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _integrationEventBehaviour.ExecuteAsync(_innerHandler, command, cancellationToken)
                .Returns(expectedResult);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.ShouldBe(expectedResult);
            await _integrationEventBehaviour.Received(1).ExecuteAsync(_innerHandler, command, cancellationToken);
        }

        [Fact]
        public async Task HandleAsync_ShouldPassCorrectParametersToBehaviour()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            CommandHandler<TestVoidCommand>? capturedHandler = null;
            TestVoidCommand? capturedCommand = null;
            CancellationToken capturedToken = default;
            
            _integrationEventBehaviour.ExecuteAsync(
                Arg.Do<CommandHandler<TestVoidCommand>>(h => capturedHandler = h),
                Arg.Do<TestVoidCommand>(c => capturedCommand = c),
                Arg.Do<CancellationToken>(t => capturedToken = t))
                .Returns(Result.Success());

            // Act
            await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            capturedHandler.ShouldBe(_innerHandler);
            capturedCommand.ShouldBe(command);
            capturedToken.ShouldBe(cancellationToken);
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnBehaviourResult()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var expectedResult = Result.Error("Modified by behaviour");
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _integrationEventBehaviour.ExecuteAsync(_innerHandler, command, cancellationToken)
                .Returns(expectedResult);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.ShouldBe(expectedResult);
            result.IsSuccess.ShouldBeFalse();
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

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }
}