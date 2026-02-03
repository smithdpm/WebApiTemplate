using Ardalis.Result;
using Cqrs.Decorators;
using Cqrs.Messaging;
using NSubstitute;
using Shouldly;

namespace Cqrs.UnitTests.Decorators;

public class CommandHandlerDecoratorTests
{
    public class CommandHandlerWithResponseTests : CommandHandlerDecoratorTests
    {
        [Fact]
        public async Task HandleInner_ShouldCallInnerHandler()
        {
            // Arrange
            var innerHandler = Substitute.For<ICommandHandler<TestCommand, string>>();
            var expectedResult = "Expected Result";
            var command = new TestCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));
            
            var decorator = new TestCommandHandlerDecorator<TestCommand, string>(innerHandler);

            // Act
            var result = await decorator.TestHandleInner(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }
    }

    public class CommandHandlerWithoutResponseTests : CommandHandlerDecoratorTests
    {
        [Fact]
        public async Task HandleInner_ShouldCallInnerHandler()
        {
            // Arrange
            var innerHandler = Substitute.For<ICommandHandler<TestCommandWithoutResponse>>();
            var command = new TestCommandWithoutResponse { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result.Success());
            
            var decorator = new TestCommandWithoutResponseHandlerDecorator<TestCommandWithoutResponse>(innerHandler);

            // Act
            var result = await decorator.TestHandleInner(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }
    }

    public class TestCommand : ICommand<string>
    {
        public Guid Id { get; set; }
    }

    public class TestCommandWithoutResponse : ICommand
    {
        public Guid Id { get; set; }
    }

    public class TestCommandHandlerDecorator<TCommand, TResponse> : CommandHandlerDecorator<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public TestCommandHandlerDecorator(ICommandHandler<TCommand, TResponse> innerHandler) 
            : base(innerHandler)
        {
        }

        public override Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            return HandleInner(command, cancellationToken);
        }

        public Task<Result<TResponse>> TestHandleInner(TCommand command, CancellationToken cancellationToken)
        {
            return HandleInner(command, cancellationToken);
        }
    }

    public class TestCommandWithoutResponseHandlerDecorator<TCommand> : CommandHandlerDecorator<TCommand>
        where TCommand : ICommand
    {
        public TestCommandWithoutResponseHandlerDecorator(ICommandHandler<TCommand> innerHandler) 
            : base(innerHandler)
        {
        }

        public override Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            return HandleInner(command, cancellationToken);
        }

        public Task<Result> TestHandleInner(TCommand command, CancellationToken cancellationToken)
        {
            return HandleInner(command, cancellationToken);
        }
    }
}