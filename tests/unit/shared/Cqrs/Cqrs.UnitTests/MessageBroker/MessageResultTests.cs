using Cqrs.MessageBroker;
using Shouldly;

namespace Cqrs.UnitTests.MessageBroker;

public class MessageResultTests
{
    [Fact]
    public void Constructor_ShouldCreateMessageResult_WithProvidedValues()
    {
        // Arrange
        var status = MessageResultStatus.DeadLetter;
        var reasonCode = "ERROR_CODE";
        var description = "Error description";
        
        // Act
        var result = new MessageResult(status, reasonCode, description);
        
        // Assert
        result.Status.ShouldBe(status);
        result.ReasonCode.ShouldBe(reasonCode);
        result.Description.ShouldBe(description);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultEmptyStrings_WhenOptionalParametersNotProvided()
    {
        // Arrange
        var status = MessageResultStatus.Success;
        
        // Act
        var result = new MessageResult(status);
        
        // Assert
        result.Status.ShouldBe(status);
        result.ReasonCode.ShouldBe(string.Empty);
        result.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public void Constructor_ShouldAllowPartialOptionalParameters()
    {
        // Arrange
        var status = MessageResultStatus.Skip;
        var reasonCode = "SKIP_REASON";
        
        // Act
        var result = new MessageResult(status, reasonCode);
        
        // Assert
        result.Status.ShouldBe(status);
        result.ReasonCode.ShouldBe(reasonCode);
        result.Description.ShouldBe(string.Empty);
    }
}