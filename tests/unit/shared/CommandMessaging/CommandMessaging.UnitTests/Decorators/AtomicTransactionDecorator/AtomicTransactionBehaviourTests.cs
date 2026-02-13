using Ardalis.Result;
using Cqrs.Decorators.AtomicTransactionDecorator;
using NSubstitute;
using SharedKernel.Database;
using Shouldly;

namespace Cqrs.UnitTests.Decorators.AtomicTransactionDecorator;

public class AtomicTransactionBehaviourTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AtomicTransactionBehaviour _behaviour;

    public AtomicTransactionBehaviourTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _behaviour = new AtomicTransactionBehaviour(_unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSaveChanges_WhenResultIsSuccessAndHasChanges()
    {
        // Arrange
        var expectedResult = Result<string>.Success("Success");
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _unitOfWork.HasChanges().Returns(true);
        
        Task<Result<string>> action() => Task.FromResult(expectedResult);

        // Act
        var result = await _behaviour.ExecuteAsync(action, cancellationToken);

        // Assert
        result.ShouldBe(expectedResult);
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSaveChanges_WhenResultIsError()
    {
        // Arrange
        var expectedResult = Result<string>.Error("Error");
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _unitOfWork.HasChanges().Returns(true);
        
        Task<Result<string>> action() => Task.FromResult(expectedResult);

        // Act
        var result = await _behaviour.ExecuteAsync(action, cancellationToken);

        // Assert
        result.ShouldBe(expectedResult);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSaveChanges_WhenNoChangesDetected()
    {
        // Arrange
        var expectedResult = Result<string>.Success("Success");
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _unitOfWork.HasChanges().Returns(false);
        
        Task<Result<string>> action() => Task.FromResult(expectedResult);

        // Act
        var result = await _behaviour.ExecuteAsync(action, cancellationToken);

        // Assert
        result.ShouldBe(expectedResult);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOriginalResult_RegardlessOfSaveOutcome()
    {
        // Arrange
        var expectedResult = Result<string>.Success("Expected");
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _unitOfWork.HasChanges().Returns(true);
        
        Task<Result<string>> action() => Task.FromResult(expectedResult);

        // Act
        var result = await _behaviour.ExecuteAsync(action, cancellationToken);

        // Assert
        result.ShouldBe(expectedResult);
        result.Value.ShouldBe("Expected");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleResultWithoutValue()
    {
        // Arrange
        var expectedResult = Result.Success();
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _unitOfWork.HasChanges().Returns(true);
        
        Task<Result> action() => Task.FromResult(expectedResult);

        // Act
        var result = await _behaviour.ExecuteAsync(action, cancellationToken);

        // Assert
        result.ShouldBe(expectedResult);
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateException_WhenActionThrows()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var cancellationToken = TestContext.Current.CancellationToken;
        
        Task<Result<string>> action() => throw expectedException;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _behaviour.ExecuteAsync(action, cancellationToken));
        
        exception.ShouldBe(expectedException);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}