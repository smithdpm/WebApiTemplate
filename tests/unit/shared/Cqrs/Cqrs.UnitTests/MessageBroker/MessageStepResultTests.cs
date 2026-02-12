using Cqrs.MessageBroker;
using Shouldly;

namespace Cqrs.UnitTests.MessageBroker;

public class MessageStepResultTests
{
    [Fact]
    public void Success_ShouldCreateResultWithSuccessStatus()
    {
        // Arrange
        var value = "test-value";
        
        // Act
        var result = MessageStepResult<string>.Success(value);
        
        // Assert
        result.Value.ShouldBe(value);
        result.Status.ShouldBe(MessageResultStatus.Success);
        result.ReasonCode.ShouldBeEmpty();
        result.Description.ShouldBeEmpty();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void DeadLetter_ShouldCreateResultWithDeadLetterStatus()
    {
        // Arrange
        var reasonCode = "ERROR_CODE";
        var description = "Error description";
        
        // Act
        var result = MessageStepResult<string>.DeadLetter(reasonCode, description);
        
        // Assert
        result.Value.ShouldBeNull();
        result.Status.ShouldBe(MessageResultStatus.DeadLetter);
        result.ReasonCode.ShouldBe(reasonCode);
        result.Description.ShouldBe(description);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void Skip_ShouldCreateResultWithSkipStatus()
    {
        // Arrange
        var reasonCode = "SKIP_CODE";
        var description = "Skip description";
        
        // Act
        var result = MessageStepResult<string>.Skip(reasonCode, description);
        
        // Assert
        result.Value.ShouldBeNull();
        result.Status.ShouldBe(MessageResultStatus.Skip);
        result.ReasonCode.ShouldBe(reasonCode);
        result.Description.ShouldBe(description);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void IsSuccess_ShouldReturnTrue_WhenStatusIsSuccess()
    {
        // Arrange & Act
        var successResult = MessageStepResult<int>.Success(42);
        var deadLetterResult = MessageStepResult<int>.DeadLetter("ERROR", "Error");
        var skipResult = MessageStepResult<int>.Skip("SKIP", "Skip");
        
        // Assert
        successResult.IsSuccess.ShouldBeTrue();
        deadLetterResult.IsSuccess.ShouldBeFalse();
        skipResult.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void ToMessageResult_ShouldConvertToMessageResult_WithSameStatusAndDetails()
    {
        // Arrange
        var successResult = MessageStepResult<string>.Success("value");
        var deadLetterResult = MessageStepResult<string>.DeadLetter("DL_CODE", "DL description");
        var skipResult = MessageStepResult<string>.Skip("SKIP_CODE", "Skip description");
        
        // Act
        var successMessage = successResult.ToMessageResult();
        var deadLetterMessage = deadLetterResult.ToMessageResult();
        var skipMessage = skipResult.ToMessageResult();
        
        // Assert
        successMessage.Status.ShouldBe(MessageResultStatus.Success);
        successMessage.ReasonCode.ShouldBeEmpty();
        successMessage.Description.ShouldBeEmpty();
        
        deadLetterMessage.Status.ShouldBe(MessageResultStatus.DeadLetter);
        deadLetterMessage.ReasonCode.ShouldBe("DL_CODE");
        deadLetterMessage.Description.ShouldBe("DL description");
        
        skipMessage.Status.ShouldBe(MessageResultStatus.Skip);
        skipMessage.ReasonCode.ShouldBe("SKIP_CODE");
        skipMessage.Description.ShouldBe("Skip description");
    }

    [Fact]
    public void Success_ShouldWorkWithComplexTypes()
    {
        // Arrange
        var complexValue = new TestComplexType 
        { 
            Id = Guid.NewGuid(), 
            Name = "Test",
            Items = new List<string> { "item1", "item2" }
        };
        
        // Act
        var result = MessageStepResult<TestComplexType>.Success(complexValue);
        
        // Assert
        result.Value.ShouldBe(complexValue);
        result.Status.ShouldBe(MessageResultStatus.Success);
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(complexValue.Id);
        result.Value.Name.ShouldBe(complexValue.Name);
        result.Value.Items.Count.ShouldBe(2);
    }

    [Fact]
    public void DeadLetter_ShouldSetDefaultValueForReferenceTypes()
    {
        // Act
        var stringResult = MessageStepResult<string>.DeadLetter("CODE", "Description");
        var objectResult = MessageStepResult<TestComplexType>.DeadLetter("CODE", "Description");
        
        // Assert
        stringResult.Value.ShouldBeNull();
        objectResult.Value.ShouldBeNull();
    }

    [Fact]
    public void DeadLetter_ShouldSetDefaultValueForValueTypes()
    {
        // Act
        var intResult = MessageStepResult<int>.DeadLetter("CODE", "Description");
        var boolResult = MessageStepResult<bool>.DeadLetter("CODE", "Description");
        var guidResult = MessageStepResult<Guid>.DeadLetter("CODE", "Description");
        
        // Assert
        intResult.Value.ShouldBe(0);
        boolResult.Value.ShouldBe(false);
        guidResult.Value.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void Skip_ShouldSetDefaultValueForReferenceTypes()
    {
        // Act
        var stringResult = MessageStepResult<string>.Skip("CODE", "Description");
        var objectResult = MessageStepResult<TestComplexType>.Skip("CODE", "Description");
        
        // Assert
        stringResult.Value.ShouldBeNull();
        objectResult.Value.ShouldBeNull();
    }

    [Fact]
    public void Skip_ShouldSetDefaultValueForValueTypes()
    {
        // Act
        var intResult = MessageStepResult<int>.Skip("CODE", "Description");
        var boolResult = MessageStepResult<bool>.Skip("CODE", "Description");
        var guidResult = MessageStepResult<Guid>.Skip("CODE", "Description");
        
        // Assert
        intResult.Value.ShouldBe(0);
        boolResult.Value.ShouldBe(false);
        guidResult.Value.ShouldBe(Guid.Empty);
    }

    private class TestComplexType
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }
}