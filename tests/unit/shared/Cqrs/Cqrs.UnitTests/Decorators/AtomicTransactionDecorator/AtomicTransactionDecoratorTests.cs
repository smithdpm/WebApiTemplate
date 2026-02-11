using Ardalis.Result;
using Cqrs.Decorators.AtomicTransactionDecorator;
using Cqrs.Messaging;
using NSubstitute;
using Shouldly;

namespace Cqrs.UnitTests.Decorators.AtomicTransactionDecorator;

public class AtomicTransactionDecoratorTests
{
    public class CommandHandlerWithResponseTests : AtomicTransactionDecoratorTests
    {
        private readonly ICommandHandler<TestCommand, string> _innerHandler;
        private readonly IAtomicTransactionBehaviour _atomicTransactionBehaviour;
        private readonly AtomicTransactionCommandDecorator<TestCommand, string> _decorator;

        public CommandHandlerWithResponseTests()
        {
            _innerHandler = Substitute.For<ICommandHandler<TestCommand, string>>();
            _atomicTransactionBehaviour = Substitute.For<IAtomicTransactionBehaviour>();
            _decorator = new AtomicTransactionCommandDecorator<TestCommand, string>(_innerHandler, _atomicTransactionBehaviour);
        }

        [Fact]
        public async Task HandleAsync_ShouldCallBehaviourExecuteAsync()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var expectedResult = Result<string>.Success("Success");
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _atomicTransactionBehaviour.ExecuteAsync(
                Arg.Any<Func<Task<Result<string>>>>(), 
                cancellationToken)
                .Returns(expectedResult);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.ShouldBe(expectedResult);
            await _atomicTransactionBehaviour.Received(1).ExecuteAsync(
                Arg.Any<Func<Task<Result<string>>>>(), 
                cancellationToken);
        }

        [Fact]
        public async Task HandleAsync_ShouldPassInnerHandlerToBehaviourExecuteAsync()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var expectedResult = Result<string>.Success("Success");
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(expectedResult);
            
            _atomicTransactionBehaviour.ExecuteAsync(
                Arg.Any<Func<Task<Result<string>>>>(), 
                cancellationToken)
                .Returns(callInfo => callInfo.ArgAt<Func<Task<Result<string>>>>(0)());

            // Act
            await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            await _innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnBehaviourResult()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var expectedResult = Result<string>.Success("Modified by behaviour");
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _atomicTransactionBehaviour.ExecuteAsync(
                Arg.Any<Func<Task<Result<string>>>>(), 
                cancellationToken)
                .Returns(expectedResult);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.ShouldBe(expectedResult);
            result.Value.ShouldBe("Modified by behaviour");
        }
    }

    public class CommandHandlerWithoutResponseTests : AtomicTransactionDecoratorTests
    {
        private readonly ICommandHandler<TestVoidCommand> _innerHandler;
        private readonly IAtomicTransactionBehaviour _atomicTransactionBehaviour;
        private readonly AtomicTransactionCommandDecorator<TestVoidCommand> _decorator;

        public CommandHandlerWithoutResponseTests()
        {
            _innerHandler = Substitute.For<ICommandHandler<TestVoidCommand>>();
            _atomicTransactionBehaviour = Substitute.For<IAtomicTransactionBehaviour>();
            _decorator = new AtomicTransactionCommandDecorator<TestVoidCommand>(_innerHandler, _atomicTransactionBehaviour);
        }

        [Fact]
        public async Task HandleAsync_ShouldCallBehaviourExecuteAsync()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var expectedResult = Result.Success();
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _atomicTransactionBehaviour.ExecuteAsync(
                Arg.Any<Func<Task<Result>>>(), 
                cancellationToken)
                .Returns(expectedResult);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

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
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var expectedResult = Result.Success();
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(expectedResult);
            
            _atomicTransactionBehaviour.ExecuteAsync(
                Arg.Any<Func<Task<Result>>>(), 
                cancellationToken)
                .Returns(callInfo => callInfo.ArgAt<Func<Task<Result>>>(0)());

            // Act
            await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            await _innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnBehaviourResult()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var expectedResult = Result.Error("Modified by behaviour");
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _atomicTransactionBehaviour.ExecuteAsync(
                Arg.Any<Func<Task<Result>>>(), 
                cancellationToken)
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
}