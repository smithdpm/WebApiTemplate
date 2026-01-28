using Ardalis.Result;
using Cqrs.Decorators;
using Cqrs.Messaging;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Shouldly;

namespace Cqrs.UnitTests.Decorators;

public class ValidationDecoratorTests
{
    public class CommandHandlerWithResponseTests : ValidationDecoratorTests
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
            
            var decorator = new ValidationDecorator<TestCommand, string>(_innerHandler, new[] { validator });

            // Act
            var result = await decorator.Handle(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Status.ShouldBe(ResultStatus.Invalid);
            result.ValidationErrors.Count().ShouldBe(1);
            result.ValidationErrors.First().ErrorMessage.ShouldBe("Name is required");
            await _innerHandler.DidNotReceive().Handle(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
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
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));
            
            var decorator = new ValidationDecorator<TestCommand, string>(_innerHandler, new[] { validator });

            // Act
            var result = await decorator.Handle(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await _innerHandler.Received(1).Handle(command, cancellationToken);
        }

        [Fact]
        public async Task Handle_ShouldCallInnerHandler_WhenNoValidatorsRegistered()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid(), Name = "Any Name" };
            var expectedResult = "Success";
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));
            
            var decorator = new ValidationDecorator<TestCommand, string>(_innerHandler, Enumerable.Empty<IValidator<TestCommand>>());

            // Act
            var result = await decorator.Handle(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await _innerHandler.Received(1).Handle(command, cancellationToken);
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
            
            var decorator = new ValidationDecorator<TestCommand, string>(_innerHandler, new[] { validator1, validator2 });

            // Act
            var result = await decorator.Handle(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Status.ShouldBe(ResultStatus.Invalid);
            result.ValidationErrors.Count().ShouldBe(2);
            result.ValidationErrors.Any(e => e.ErrorMessage == "Name is required").ShouldBeTrue();
            result.ValidationErrors.Any(e => e.ErrorMessage == "Id must be valid").ShouldBeTrue();
            await _innerHandler.DidNotReceive().Handle(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
        }
    }

    public class CommandHandlerWithoutResponseTests : ValidationDecoratorTests
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
            
            var decorator = new ValidationDecorator<TestVoidCommand>(_innerHandler, new[] { validator });

            // Act
            var result = await decorator.Handle(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Status.ShouldBe(ResultStatus.Invalid);
            result.ValidationErrors.Count().ShouldBe(1);
            result.ValidationErrors.First().ErrorMessage.ShouldBe("Name is required");
            await _innerHandler.DidNotReceive().Handle(Arg.Any<TestVoidCommand>(), Arg.Any<CancellationToken>());
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
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(Result.Success());
            
            var decorator = new ValidationDecorator<TestVoidCommand>(_innerHandler, new[] { validator });

            // Act
            var result = await decorator.Handle(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _innerHandler.Received(1).Handle(command, cancellationToken);
        }

        [Fact]
        public async Task Handle_ShouldSkipValidation_WhenNoValidatorsRegistered()
        {
            // Arrange
            var command = new TestVoidCommand { Id = Guid.NewGuid(), Name = "Any Name" };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            _innerHandler.Handle(command, cancellationToken)
                .Returns(Result.Success());
            
            var decorator = new ValidationDecorator<TestVoidCommand>(_innerHandler, Enumerable.Empty<IValidator<TestVoidCommand>>());

            // Act
            var result = await decorator.Handle(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _innerHandler.Received(1).Handle(command, cancellationToken);
        }
    }

    public class ValidationLogicTests : ValidationDecoratorTests
    {
        [Fact]
        public async Task ValidateAsync_ShouldReturnEmptyList_WhenNoValidators()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid(), Name = "Test" };
            var validators = Enumerable.Empty<IValidator<TestCommand>>();
            var cancellationToken = TestContext.Current.CancellationToken;

            // Act
            var result = await ValidationLogic.ValidateAsync(command, validators, cancellationToken);

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(0);
        }

        [Fact]
        public async Task ValidateAsync_ShouldReturnEmptyList_WhenAllValidatorsPass()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid(), Name = "Valid Name" };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            var validator1 = Substitute.For<IValidator<TestCommand>>();
            validator1.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken)
                .Returns(new ValidationResult());
            
            var validator2 = Substitute.For<IValidator<TestCommand>>();
            validator2.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken)
                .Returns(new ValidationResult());
            
            var validators = new[] { validator1, validator2 };

            // Act
            var result = await ValidationLogic.ValidateAsync(command, validators, cancellationToken);

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(0);
        }

        [Fact]
        public async Task ValidateAsync_ShouldReturnErrors_WhenValidationFails()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid(), Name = "" };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            var validator = Substitute.For<IValidator<TestCommand>>();
            validator.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken)
                .Returns(new ValidationResult(new[] 
                { 
                    new ValidationFailure("Name", "Name is required"),
                    new ValidationFailure("Id", "Id is invalid")
                }));
            
            var validators = new[] { validator };

            // Act
            var result = await ValidationLogic.ValidateAsync(command, validators, cancellationToken);

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);
            result.Any(e => e.ErrorMessage == "Name is required").ShouldBeTrue();
            result.Any(e => e.ErrorMessage == "Id is invalid").ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateAsync_ShouldRunAllValidators_WhenMultipleProvided()
        {
            // Arrange
            var command = new TestCommand { Id = Guid.NewGuid(), Name = "" };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            var validator1 = Substitute.For<IValidator<TestCommand>>();
            validator1.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken)
                .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Error from validator 1") }));
            
            var validator2 = Substitute.For<IValidator<TestCommand>>();
            validator2.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken)
                .Returns(new ValidationResult(new[] { new ValidationFailure("Id", "Error from validator 2") }));
            
            var validators = new[] { validator1, validator2 };

            // Act
            var result = await ValidationLogic.ValidateAsync(command, validators, cancellationToken);

            // Assert
            await validator1.Received(1).ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken);
            await validator2.Received(1).ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken);
            result.Count().ShouldBe(2);
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