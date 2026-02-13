using Cqrs.Extensions;
using Cqrs.Operations.Commands;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Shouldly;

namespace Cqrs.UnitTests.Extensions;


public class ValidationExtensionTests
{
    [Fact]
    public async Task ValidateAsync_ShouldReturnEmptyList_WhenNoValidators()
    {
        // Arrange
        var command = new TestCommand { Id = Guid.NewGuid(), Name = "Test" };
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await ValidationExtensions.ValidateAsync(command, validators, cancellationToken);

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
        var result = await ValidationExtensions.ValidateAsync(command, validators, cancellationToken);

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
        var result = await ValidationExtensions.ValidateAsync(command, validators, cancellationToken);

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
        var result = await ValidationExtensions.ValidateAsync(command, validators, cancellationToken);

        // Assert
        await validator1.Received(1).ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken);
        await validator2.Received(1).ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), cancellationToken);
        result.Count().ShouldBe(2);
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