using Ardalis.Result;
using Cqrs.Decorators.ValidationDecorator;
using Cqrs.Messaging;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Shouldly;

namespace Cqrs.UnitTests.Decorators;

public partial class ValidationCommandDecoratorTests
{
    public class CommandHandlerWithResponseTests
    {
        private readonly ICommandHandler<TestCommand, string> _innerHandler;

        public CommandHandlerWithResponseTests()
        {
            _innerHandler = Substitute.For<ICommandHandler<TestCommand, string>>();
        }

        [Fact]
        public async Task Handle_ShouldReturnInvalid_WhenValidationFails()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid(), Name = "" };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            var validator = Substitute.For<IValidator<TestCommand>>();
            validator.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken)
                .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));
            
            var decorator = new ValidationCommandDecorator<TestCommand, string>(_innerHandler, new[] { validator });

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Status.ShouldBe(ResultStatus.Invalid);
            result.ValidationErrors.Count().ShouldBe(1);
            result.ValidationErrors.First().ErrorMessage.ShouldBe("Name is required");
            await _innerHandler.DidNotReceive().HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldCallInnerHandler_WhenValidationPasses()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid(), Name = "Valid Name" };
            var expectedResult = "Success";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            var validator = Substitute.For<IValidator<TestCommand>>();
            validator.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken)
                .Returns(new ValidationResult());
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));
            
            var decorator = new ValidationCommandDecorator<TestCommand, string>(_innerHandler, new[] { validator });

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await _innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }

        [Fact]
        public async Task Handle_ShouldCallInnerHandler_WhenNoValidatorsRegistered()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid(), Name = "Any Name" };
            var expectedResult = "Success";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));
            
            var decorator = new ValidationCommandDecorator<TestCommand, string>(_innerHandler, Enumerable.Empty<IValidator<TestCommand>>());

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await _innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }

        [Fact]
        public async Task Handle_ShouldAggregateErrors_WhenMultipleValidatorsFail()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid(), Name = "" };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            var validator1 = Substitute.For<IValidator<TestCommand>>();
            validator1.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken)
                .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));
            
            var validator2 = Substitute.For<IValidator<TestCommand>>();
            validator2.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken)
                .Returns(new ValidationResult(new[] { new ValidationFailure("Id", "Id must be valid") }));
            
            var decorator = new ValidationCommandDecorator<TestCommand, string>(_innerHandler, new[] { validator1, validator2 });

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Status.ShouldBe(ResultStatus.Invalid);
            result.ValidationErrors.Count().ShouldBe(2);
            result.ValidationErrors.Any(e => e.ErrorMessage == "Name is required").ShouldBeTrue();
            result.ValidationErrors.Any(e => e.ErrorMessage == "Id must be valid").ShouldBeTrue();
            await _innerHandler.DidNotReceive().HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
        }
    }

    public class CommandHandlerWithoutResponseTests
    {
        private readonly ICommandHandler<TestVoidCommand> _innerHandler;

        public CommandHandlerWithoutResponseTests()
        {
            _innerHandler = Substitute.For<ICommandHandler<TestVoidCommand>>();
        }

        [Fact]
        public async Task Handle_ShouldReturnInvalid_WhenValidationFails()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid(), Name = "" };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            var validator = Substitute.For<IValidator<TestVoidCommand>>();
            validator.ValidateAsync(Arg.Any<ValidationContext<TestVoidCommand>>(), cancellationToken)
                .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));
            
            var decorator = new ValidationCommandDecorator<TestVoidCommand>(_innerHandler, new[] { validator });

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Status.ShouldBe(ResultStatus.Invalid);
            result.ValidationErrors.Count().ShouldBe(1);
            result.ValidationErrors.First().ErrorMessage.ShouldBe("Name is required");
            await _innerHandler.DidNotReceive().HandleAsync(Arg.Any<TestVoidCommand>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldCallInnerHandler_WhenValidationPasses()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid(), Name = "Valid Name" };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            var validator = Substitute.For<IValidator<TestVoidCommand>>();
            validator.ValidateAsync(Arg.Any<ValidationContext<TestVoidCommand>>(), cancellationToken)
                .Returns(new ValidationResult());
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result.Success());
            
            var decorator = new ValidationCommandDecorator<TestVoidCommand>(_innerHandler, new[] { validator });

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }

        [Fact]
        public async Task Handle_ShouldSkipValidation_WhenNoValidatorsRegistered()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid(), Name = "Any Name" };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.HandleAsync(command, cancellationToken)
                .Returns(Result.Success());
            
            var decorator = new ValidationCommandDecorator<TestVoidCommand>(_innerHandler, Enumerable.Empty<IValidator<TestVoidCommand>>());

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _innerHandler.Received(1).HandleAsync(command, cancellationToken);
        }
    }

    public class TestCommand : ICommand<string>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TestVoidCommand : ICommand
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}