using Ardalis.Result;
using Cqrs.Decorators;
using Cqrs.Decorators.AtomicTransactionDecorator;
using Cqrs.Messaging;
using NSubstitute;
using SharedKernel.Database;
using Shouldly;

namespace Cqrs.UnitTests.Decorators;

public class AtomicTransactionDecoratorTests
{
    public class CommandHandlerWithResponseTests : AtomicTransactionDecoratorTests
    {
        private readonly ICommandHandler<TestCommand, string> _innerHandler;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AtomicTransactionCommandDecorator<TestCommand, string> _decorator;
        private readonly IAtomicTransactionBehaviour _atomicTransactionBehaviour;

        public CommandHandlerWithResponseTests()
        {
            _innerHandler = Substitute.For<ICommandHandler<TestCommand, string>>();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _atomicTransactionBehaviour = new AtomicTransactionBehaviour(_unitOfWork);
            _decorator = new AtomicTransactionCommandDecorator<TestCommand, string>(_innerHandler, _atomicTransactionBehaviour);
        }

        [Fact]
        public async Task Handle_ShouldSaveChanges_WhenCommandSucceedsAndHasChanges()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var expectedResult = "Success";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));
            _unitOfWork.HasChanges().Returns(true);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
        }

        [Fact]
        public async Task Handle_ShouldNotSaveChanges_WhenCommandFails()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var errorMessage = "Command failed";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result<string>.Error(errorMessage));
            _unitOfWork.HasChanges().Returns(true);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldNotSaveChanges_WhenNoChangesDetected()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid() };
            var expectedResult = "Success";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));
            _unitOfWork.HasChanges().Returns(false);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
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
            _unitOfWork.HasChanges().Returns(false);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await _innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }
    }

    public class CommandHandlerWithoutResponseTests : AtomicTransactionDecoratorTests
    {
        private readonly ICommandHandler<TestVoidCommand> _innerHandler;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AtomicTransactionCommandDecorator<TestVoidCommand> _decorator;
        private readonly IAtomicTransactionBehaviour _atomicTransactionBehaviour;

        public CommandHandlerWithoutResponseTests()
        {
            _innerHandler = Substitute.For<ICommandHandler<TestVoidCommand>>();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _atomicTransactionBehaviour = new AtomicTransactionBehaviour(_unitOfWork);
            _decorator = new AtomicTransactionCommandDecorator<TestVoidCommand>(_innerHandler, _atomicTransactionBehaviour);
        }

        [Fact]
        public async Task Handle_ShouldSaveChanges_WhenCommandSucceedsAndHasChanges()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result.Success());
            _unitOfWork.HasChanges().Returns(true);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
        }

        [Fact]
        public async Task Handle_ShouldNotSaveChanges_WhenCommandFails()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var errorMessage = "Command failed";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result.Error(errorMessage));
            _unitOfWork.HasChanges().Returns(true);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldNotSaveChanges_WhenNoChangesDetected()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result.Success());
            _unitOfWork.HasChanges().Returns(false);

            // Act
            var result = await _decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
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